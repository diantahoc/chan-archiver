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
        public static string api_cache_dir = "";
        public static string post_files_dir = "";

        public static string program_dir;

        static string board = "g";

        public static Amib.Threading.SmartThreadPool thumb_stp;
        public static Amib.Threading.SmartThreadPool file_stp;

        static List<int> monitored = new List<int>();

        public static AniWrap.AniWrap aw;

        static bool thumb_only = false;

        static bool use_wget = false;

        public static bool verbose = false;

        static string wget_path = "/usr/bin/wget";

        public static Dictionary<string, BoardWatcher> active_dumpers = new Dictionary<string, BoardWatcher>();

        public static Dictionary<string, FileQueueStateInfo> queued_files = new Dictionary<string, FileQueueStateInfo>();

        public static DateTime StartUpTime = DateTime.Now;

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

                if (arg == "--thumbonly")
                {
                    thumb_only = true;
                }

                if (arg == "--verbose")
                {
                    verbose = true;
                }

                if (arg == "--wget")
                {
                    //if (Environment.OSVersion.Platform == PlatformID.Unix)
                    //{
                    //    if (File.Exists(wget_path))
                    //    {
                    //        use_wget = true;
                    //    }
                    //}
                    Console.WriteLine("Sorry, wget backend is dropped");
                }

            }

            thumb_stp = new Amib.Threading.SmartThreadPool() { MaxThreads = 20, MinThreads = 0 };
            thumb_stp.Start();

            file_stp = new Amib.Threading.SmartThreadPool() { MaxThreads = 10, MinThreads = 0 };
            file_stp.Start();

            program_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "chanarchiver");
            Directory.CreateDirectory(program_dir);

            file_save_dir = Path.Combine(program_dir, "files");
            thumb_save_dir = Path.Combine(program_dir, "thumbs");
            post_files_dir = Path.Combine(program_dir, "posts");
            api_cache_dir = Path.Combine(program_dir, "aniwrap_cache");

            Directory.CreateDirectory(file_save_dir);
            Directory.CreateDirectory(thumb_save_dir);
            Directory.CreateDirectory(post_files_dir);
            Directory.CreateDirectory(api_cache_dir);

            aw = new AniWrap.AniWrap(api_cache_dir);

            Console.WriteLine("ChanARCHIVER stable 0.61");

            Console.WriteLine(string.Format("Saving files in '{0}'", program_dir));

            if (thumb_only) 
            {
                Console.WriteLine("ChanARCHIVER is running in thumbnail only mode");
            }

            if (server)
            {
                Console.WriteLine("ChanARCHIVER is running in server mode");
                start_server();
            }
            else
            {
                if (!force_no_server)
                {
                    start_server();
                }

                if (single_id > 0)
                {
                    archive_single(board, single_id);
                }
                else
                {
                    archive_board(board);
                }
            }
            Console.WriteLine("Enter Q to exit");
            while (Console.ReadLine().ToUpper() != "Q") { }
        }

        public static void archive_single(string board, int id)
        {
            if (active_dumpers.ContainsKey(board))
            {
                BoardWatcher bw = active_dumpers[board];
                bw.AddThreadId(id);
            }
            else
            {
                BoardWatcher bw = get_board_watcher(board);
                active_dumpers.Add(board, bw);
                bw.AddThreadId(id);
            }
        }

        private static BoardWatcher get_board_watcher(string board)
        {
            BoardWatcher b = new BoardWatcher(board);
            return b;
        }

        public static void archive_board(string board)
        {
            if (active_dumpers.ContainsKey(board))
            {
                BoardWatcher bw = active_dumpers[board];
                bw.StartFullMode();
            }
            else
            {
                BoardWatcher bw = get_board_watcher(board);
                active_dumpers.Add(board, bw);
                bw.StartFullMode();
            }
        }

        private static void start_server()
        {
            try
            {
                HttpServer.HttpServer server = new HttpServer.HttpServer();

                server.ServerName = "ChanARCHIVER";

                server.Add(new ChanArchiver.HttpServerHandlers.OverviewPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.LogPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileQueuePageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.WatchJobsPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.ResourcesHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileHandler());

                server.Add(new ThreadServerModule());
                Console.WriteLine("Starting HTTP server...");
                server.Start(IPAddress.Any, 8787);
                Console.WriteLine("Listening on port 8787");
            }
            catch (Exception ex)
            {
                Console.Beep();
                Console.WriteLine("Could not start HTTP server: {0}. \n Will now exit", ex.Message);
                Console.Read();
            }
        }

        public static void dump_files(PostFile pf)
        {
            string md5 = base64tostring(pf.hash);
            string file_path = Path.Combine(file_save_dir, md5 + "." + pf.ext);
            string thumb_path = Path.Combine(thumb_save_dir, md5 + ".jpg");

            string reff = string.Format("http://boards.4chan.org/{0}/res/{1}", pf.board, pf.owner);

            if (!File.Exists(thumb_path))
            {
                if (!queued_files.ContainsKey("thumb" + md5))
                {
                    queued_files.Add("thumb" + md5, new FileQueueStateInfo(md5) { Type = FileQueueStateInfo.FileType.Thumbnail, Url = pf.ThumbLink });

                    thumb_stp.QueueWorkItem(new Amib.Threading.Action((Action)delegate
                    {
                        download_file(new string[] { thumb_path, pf.ThumbLink, reff, "thumb" + md5 });
                    }), Amib.Threading.WorkItemPriority.Highest);
                }

            }

            if (!thumb_only)
            {
                if (!File.Exists(file_path))
                {
                    if (!queued_files.ContainsKey("file" + md5))
                    {
                        queued_files.Add("file" + md5, new FileQueueStateInfo(md5) { Type = FileQueueStateInfo.FileType.FullFile, Url = pf.FullImageLink });

                        file_stp.QueueWorkItem(new Amib.Threading.Action((Action)delegate
                        {
                            download_file(new string[] { file_path, pf.FullImageLink, reff, "file" + md5 });
                        }), Amib.Threading.WorkItemPriority.Normal);
                    }
                }
            }
        }

        private static void download_file(string[] param)
        {
            string user_agent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:26.0) Gecko/20100101 Firefox/26.0";

            string save_path = param[0];
            string url = param[1];
            string referer = param[2];

            string key = param[3];

            FileQueueStateInfo f = get_file_state(key);

            if (use_wget)
            {
                System.Diagnostics.ProcessStartInfo psr = new System.Diagnostics.ProcessStartInfo(wget_path);


                psr.UseShellExecute = false;
                psr.CreateNoWindow = true;
                psr.RedirectStandardError = true;
                psr.RedirectStandardOutput = true;
                psr.Arguments = string.Format("-U \"{0}\" -–referer=\"{1}\" -O \"{2}\" {3}", user_agent, referer, save_path, url);

                using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(psr))
                {
                    f.Status = FileQueueStateInfo.DownloadStatus.Downloading;
                    p.WaitForExit();
                    return;
                }
            }
            else
            {

                using (WebClient nc = new WebClient())
                {
                    nc.Headers.Add(HttpRequestHeader.UserAgent, user_agent);
                    nc.Headers.Add(HttpRequestHeader.Referer, referer);
                    f.Status = FileQueueStateInfo.DownloadStatus.Downloading;

                    int count = 0;
                    while (true)
                    {
                        f.RetryCount = count;

                        if (count >= 30)
                        {
                            Program.LogMessage(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Fail,
                                Message = string.Format("Failed to download file '{0}', exceeded retry count of 30", f.Url),
                                Sender = "FileDumper",
                                Title = "-"
                            });
                            break;
                        }

                        //  byte[] file_data = null;

                        try
                        {

                            using (Stream s = nc.OpenRead(url))
                            {
                                WebHeaderCollection whc = nc.ResponseHeaders;

                                string content_length = whc[HttpResponseHeader.ContentLength];

                                double total_length = Convert.ToDouble(content_length);

                                f.Length = total_length;

                                int b_s = 0;

                                byte[] buffer = new byte[2048];

                                using (FileStream fs = new FileStream(save_path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                {
                                    while ((b_s = s.Read(buffer, 0, 2048)) > 0)
                                    {
                                        fs.Write(buffer, 0, b_s);
                                        f.Downloaded += Convert.ToDouble(b_s);
                                        if (f.Type == FileQueueStateInfo.FileType.Thumbnail) { NetworkUsageCounter.ThumbConsumed += b_s; }
                                        if (f.Type == FileQueueStateInfo.FileType.FullFile) { NetworkUsageCounter.FileConsumed += b_s; }
                                    }
                                }
                            }

                            Program.LogMessage(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Success,
                                Message = string.Format("Downloaded file '{0}' successfully", f.Url),
                                Sender = "FileDumper",
                                Title = "-"
                            });


                            /* file_data = nc.DownloadData(url);

                             if (length > 0)
                             {
                                 if (file_data.Length != length)
                                 {
                                     //corrupte file, redownload.

                                     if (file_type == "thumb") { NetworkUsageCounter.ThumbConsumed += file_data.Length; }
                                     if (file_type == "file") { NetworkUsageCounter.FileConsumed += file_data.Length; }

                                     count++;
                                     continue;
                                 }
                             }

                             File.WriteAllBytes(save_path, file_data);

                             if (file_type == "thumb") { NetworkUsageCounter.ThumbConsumed += file_data.Length; }
                             if (file_type == "file") { NetworkUsageCounter.FileConsumed += file_data.Length; }*/

                            break;
                        }
                        catch (Exception ex)
                        {

                            Program.LogMessage(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Warning,
                                Message = string.Format("Error occured while downloading file '{0}': {1} @ {2}", f.Url, ex.Message, ex.StackTrace),
                                Sender = "FileDumper",
                                Title = "-"
                            });

                            count++;
                        }
                    }

                    queued_files.Remove(key);
                    return;
                }
            }
        }

        private static FileQueueStateInfo get_file_state(string hash)
        {
            if (queued_files.ContainsKey(hash))
            {
                return queued_files[hash];

            }
            else
            {
                FileQueueStateInfo a = new FileQueueStateInfo(hash);
                queued_files.Add(hash, a);
                return a;
            }
        }

        public static string base64tostring(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in Convert.FromBase64String(s))
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        public static string format_size_string(double size)
        {
            double KB = 1024;
            double MB = 1048576;
            double GB = 1073741824;
            if (size < KB)
            {
                return size.ToString() + " B";
            }
            else if (size > KB & size < MB)
            {
                return Math.Round(size / KB, 2).ToString() + " KB";
            }
            else if (size > MB & size < GB)
            {
                return Math.Round(size / MB, 2).ToString() + " MB";
            }
            else if (size > GB)
            {
                return Math.Round(size / GB, 2).ToString() + " GB";
            }
            else
            {
                return Convert.ToString(size);
            }
        }

        public static List<LogEntry> logs = new List<LogEntry>();

        public static void LogMessage(LogEntry entry)
        {
            if (verbose)
            {
                //[Level] Time (sender - title) : message
                Console.WriteLine("[{0}] {1} {2}: {3}", entry.Level.ToString(), entry.Time.ToShortDateString() + entry.Time.ToShortTimeString(), entry.Title == "-" ? string.Format("({0})", entry.Sender) : string.Format("({0} - {1})", entry.Sender, entry.Title), entry.Message);
            }

            logs.Add(entry);
        }

    }
}
