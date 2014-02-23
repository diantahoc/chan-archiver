using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AniWrap.DataTypes;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;

namespace ChanArchiver
{
    class Program
    {
        public static string file_save_dir = "";
        public static string thumb_save_dir = "";

        public static string program_dir;

        static string board = "g";

        public static Amib.Threading.SmartThreadPool stp;

        static List<int> monitored = new List<int>();

        static AniWrap.AniWrap aw;

        static int thread_worker_interval = 3;

        static void Main(string[] args)
        {
            int single_id = -1;

            bool server = false;
            bool force_no_server = false;
            foreach (string arg in args)
            {
                if (arg.StartsWith("--thread"))
                {
                    // --thread:board:id
                    board = arg.Split(':')[1];
                    single_id = Convert.ToInt32(arg.Split(':')[2]);
                }

                if (arg == "--server")
                {
                    server = true;
                }

                if (arg == "--noserver")
                {
                    force_no_server = true;
                }

                if (arg.StartsWith("--board"))
                {
                    board = arg.Split(':')[1];
                }

            }

            stp = new Amib.Threading.SmartThreadPool();
            stp.MinThreads = 0;
            stp.MaxThreads = 25;
            stp.Start();

            program_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "chanarchiver");
            Directory.CreateDirectory(program_dir);

            file_save_dir = Path.Combine(program_dir, "files");
            thumb_save_dir = Path.Combine(program_dir, "thumbs");

            Directory.CreateDirectory(file_save_dir);
            Directory.CreateDirectory(thumb_save_dir);

            Console.WriteLine("ChanARCHIVER unstable 0.4");
            Console.WriteLine("Powered by AniWrap Library");

            Console.WriteLine(string.Format("Saving files in '{0}'", program_dir));

            if (server)
            {
                Console.WriteLine("ChanARCHIVER is running in server mode");

                start_server();
                Console.WriteLine("Enter Q to exit");
                while (Console.ReadLine().ToUpper() != "Q") { }
                return;
            }
            else
            {
                if (!force_no_server)
                {
                    Console.WriteLine("Starting HTTP server...");
                    start_server();
                    Console.WriteLine("Listening on port 8787");
                }

                string animwrap_c = Path.Combine(program_dir, "aniwrap_cache");
                Directory.CreateDirectory(animwrap_c);
                aw = new AniWrap.AniWrap(animwrap_c);

                if (single_id > 0)
                {
                    Console.WriteLine("Archiving thread '{0}' in board /{1}/", single_id, board);
                    thread_worker_interval = 1;
                    update_thread_worker(single_id);
                }
                else
                {
                    Console.WriteLine("Archiving board : /" + board + "/");

                    while (true)
                    {
                        CatalogItem[][] catalog = aw.GetCatalog(board);

                        foreach (CatalogItem[] page in catalog)
                        {
                            foreach (CatalogItem thread in page)
                            {

                                if (!monitored.Contains(thread.ID))
                                {
                                    monitored.Add(thread.ID);
                                    Console.WriteLine("Found new thread " + thread.ID);

                                    Task.Factory.StartNew((Action)delegate
                                    {
                                        update_thread_worker(thread.ID);
                                    });
                                    System.Threading.Thread.Sleep(5000); //allow 5 secs between each worker launch
                                }
                            }

                            System.Threading.Thread.Sleep(10 * 1000); //allow 10 seconds between each page.
                        }

                        System.Threading.Thread.Sleep(10 * 60 * 1000); //refresh catalog each 10 mins
                    }
                }
            }
        }

        private static void start_server()
        {
            HttpServer.HttpServer server = new HttpServer.HttpServer();

            server.ServerName = "ChanARCHIVER";

            server.Add(new ThreadServerModule());

            server.Start(IPAddress.Any, 8787);
        }

        private static void update_thread_worker(int tid)
        {
            string thread_folder = Path.Combine(program_dir, board, tid.ToString());

            Directory.CreateDirectory(thread_folder);

            while (true)
            {
                ThreadContainer tc = null;
                try
                {
                    Console.WriteLine("Updating thread " + tid.ToString());

                    tc = aw.GetThreadData(board, tid);

                    string op = Path.Combine(thread_folder, "op.json");

                    if (!File.Exists(op))
                    {
                        string post_data = get_post_string(tc.Instance);

                        if (tc.Instance.File != null) { dump_files(tc.Instance.File); }

                        File.WriteAllText(op, post_data);
                    }

                    int count = tc.Replies.Count();

                    for (int i = 0; i < count; i++)
                    {
                        string item_path = Path.Combine(thread_folder, tc.Replies[i].ID.ToString() + ".json");
                        if (!File.Exists(item_path))
                        {
                            string post_data = get_post_string(tc.Replies[i]);

                            if (tc.Replies[i].File != null) { dump_files(tc.Replies[i].File); }
                            File.WriteAllText(item_path, post_data);
                        }
                    }

                    Console.WriteLine("Updated thread number " + tid.ToString());
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        //thread died
                        //monitored.Remove(id);
                        Console.WriteLine("Thread " + tid.ToString() + " 404'ed");
                        break;
                    }
                }
                System.Threading.Thread.Sleep(thread_worker_interval * 60 * 1000); // refresh thread worker each X min
            }
        }

        private static string get_post_string(GenericPost gp)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            if (gp.GetType() == typeof(AniWrap.DataTypes.Thread))
            {
                AniWrap.DataTypes.Thread t = (AniWrap.DataTypes.Thread)gp;
                dic.Add("Closed", t.IsClosed);
                dic.Add("Sticky", t.IsSticky);
            }

            dic.Add("Board", gp.Board);

            dic.Add("ID", gp.ID);

            dic.Add("Name", gp.Name);

            if (gp.Capcode != GenericPost.CapcodeEnum.None)
            {
                dic.Add("Capcode", gp.Capcode);
            }

            if (!string.IsNullOrEmpty(gp.Comment))
            {
                dic.Add("RawComment", gp.Comment);

                dic.Add("FormattedComment", gp.CommentText);
            }
            /*// Flag stuffs*/
            if (!string.IsNullOrEmpty(gp.country_flag))
            {
                dic.Add("CountryFlag", gp.country_flag);

            }

            if (!string.IsNullOrEmpty(gp.country_name))
            {
                dic.Add("CountryName", gp.country_name);
            }
            /* Flag stuffs //*/

            if (!string.IsNullOrEmpty(gp.Email))
            {
                dic.Add("Email", gp.Email);
            }

            if (!string.IsNullOrEmpty(gp.Trip))
            {
                dic.Add("Trip", gp.Trip);
            }

            if (!string.IsNullOrEmpty(gp.Subject))
            {
                dic.Add("Subject", gp.Subject);
            }

            if (!string.IsNullOrEmpty(gp.PosterID))
            {
                dic.Add("PosterID", gp.PosterID);
            }

            dic.Add("Time", gp.Time);

            if (gp.File != null)
            {
                dic.Add("FileHash", base64tostring(gp.File.hash));
                dic.Add("FileName", gp.File.filename + "." + gp.File.ext);
                dic.Add("ThumbTime", gp.File.thumbnail_tim);
                dic.Add("FileHeight", gp.File.height);
                dic.Add("FileWidth", gp.File.width);
                dic.Add("FileSize", gp.File.size);

            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(dic, Newtonsoft.Json.Formatting.Indented);
        }

        //static List<string> files_in_progress = new List<string>();
        //static List<string> thumb_in_progress = new List<string>();

        public static void dump_files(PostFile pf)
        {
            string md5 = base64tostring(pf.hash);
            string file_path = Path.Combine(file_save_dir, md5 + "." + pf.ext);
            string thumb_path = Path.Combine(thumb_save_dir, md5 + ".jpg");

            string reff = string.Format("http://boards.4chan.org/{0}/res/{1}", pf.board, pf.owner);

            if (!File.Exists(thumb_path))
            {
                stp.QueueWorkItem(new Amib.Threading.Action((Action)delegate
                {
                    download_file(new string[] { thumb_path, pf.ThumbLink, reff });
                }), Amib.Threading.WorkItemPriority.AboveNormal);

            }

            if (!File.Exists(file_path))
            {
                stp.QueueWorkItem(new Amib.Threading.Action((Action)delegate
                {
                    download_file(new string[] { file_path, pf.FullImageLink, reff });
                }), Amib.Threading.WorkItemPriority.Normal);
            }

        }

        private static void download_file(string[] param)
        {
            string save_path = param[0];
            string url = param[1];

            using (WebClient nc = new WebClient())
            {
                nc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:26.0) Gecko/20100101 Firefox/26.0");
                nc.Headers.Add(HttpRequestHeader.Referer, param[2]);

                int count = 0;
                while (count < 20)
                {
                    
                    byte[] file_data = null;

                    try
                    {
                        file_data = nc.DownloadData(url);

                        int length = -1;

                        Int32.TryParse(Convert.ToString(nc.ResponseHeaders[HttpRequestHeader.ContentLength]), out length);

                        if (length > 0)
                        {
                            if (file_data.Length != length)
                            {
                                //corrupte file, redownload.
                                count++;
                                continue;
                            }
                        }

                        File.WriteAllBytes(save_path, file_data);
                        break;
                    }
                    catch (Exception)
                    {
                        count++;
                    }
                }
            }
        }

        private static string base64tostring(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in Convert.FromBase64String(s))
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString().ToLower();
        }


    }
}
