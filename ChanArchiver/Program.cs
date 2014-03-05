﻿using System;
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
        public static string temp_files_dir = "";

        public static string program_dir;

        static string board = "g";

        public static Amib.Threading.SmartThreadPool thumb_stp;
        public static Amib.Threading.SmartThreadPool file_stp;

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
                if (arg.StartsWith("--thread:"))
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

                if (arg.StartsWith("--board:"))
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

                if (arg.StartsWith("--savedir"))
                {
                    program_dir = args[Array.IndexOf(args, arg) + 1];
                }

                if (arg == "-help" || arg == "--help" || arg == "/help" || arg == "-h")
                {
                    Console.Write("Help text");
                    return;
                }

            }

            thumb_stp = new Amib.Threading.SmartThreadPool() { MaxThreads = 20, MinThreads = 0 };
            thumb_stp.Start();

            file_stp = new Amib.Threading.SmartThreadPool() { MaxThreads = 10, MinThreads = 0 };
            file_stp.Start();

            if (string.IsNullOrEmpty(program_dir))
            {
                program_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "chanarchiver");
            }

            Directory.CreateDirectory(program_dir);

            file_save_dir = Path.Combine(program_dir, "files");
            thumb_save_dir = Path.Combine(program_dir, "thumbs");
            post_files_dir = Path.Combine(program_dir, "posts");
            api_cache_dir = Path.Combine(program_dir, "aniwrap_cache");
            temp_files_dir = Path.Combine(program_dir, "temp");

            Directory.CreateDirectory(file_save_dir);
            Directory.CreateDirectory(thumb_save_dir);
            Directory.CreateDirectory(post_files_dir);
            Directory.CreateDirectory(api_cache_dir);
            Directory.CreateDirectory(temp_files_dir);

            aw = new AniWrap.AniWrap(api_cache_dir);

            Console.Title = "ChanArchiver";

            print("ChanArchiver", ConsoleColor.Cyan);
            Console.WriteLine(" v0.69 semi-stable");

            Console.Write("Saving files in ");
            print(string.Format("'{0}'\n", program_dir), ConsoleColor.Red);

            // Console.WriteLine(string.Format("Saving files in '{0}'", program_dir));

            load_stats();

            if (thumb_only)
            {
                Console.WriteLine("ChanArchiver is running in thumbnail only mode");
            }

            if (server)
            {
                Console.WriteLine("ChanArchiver is running in server mode");
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

            save_stats();
        }

        private static void load_stats()
        {
            string stats_file = Path.Combine(program_dir, "stats.log");
            if (File.Exists(stats_file))
            {
                string[] data = File.ReadAllLines(stats_file);

                //0: api consumed
                //1: file consumed
                //2: thumb consumed

                NetworkUsageCounter.ApiConsumed += Convert.ToDouble(data[0]);
                NetworkUsageCounter.FileConsumed += Convert.ToDouble(data[1]);
                NetworkUsageCounter.ThumbConsumed += Convert.ToDouble(data[2]);
            }
        }

        private static void save_stats()
        {
            string stats_file = Path.Combine(program_dir, "stats.log");

            File.WriteAllLines(stats_file, new string[] 
            { 
                NetworkUsageCounter.ApiConsumed.ToString(),
                NetworkUsageCounter.FileConsumed.ToString(),
                NetworkUsageCounter.ThumbConsumed.ToString()
            });
        }

        private static void print(string text, ConsoleColor color)
        {
            ConsoleColor old_c = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = old_c;
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

                server.ServerName = "ChanArchiver";

                server.Add(new ChanArchiver.HttpServerHandlers.OverviewPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.LogPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileQueuePageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.WatchJobsPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileInfoPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.ResourcesHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileHandler());

                server.Add(new ThreadServerModule());
                Console.WriteLine("Starting HTTP server...");
                server.Start(IPAddress.Any, 8787);
                Console.WriteLine("Listening on port 8787. \nWebsite is accessible at (http://*:8787)");
            }
            catch (Exception ex)
            {
                Console.Beep();
                Console.WriteLine("Could not start HTTP server: {0}.", ex.Message);
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
                    queued_files.Add("thumb" + md5, new FileQueueStateInfo(md5, pf) { Type = FileQueueStateInfo.FileType.Thumbnail, Url = pf.ThumbLink });

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
                        queued_files.Add("file" + md5, new FileQueueStateInfo(md5, pf) { Type = FileQueueStateInfo.FileType.FullFile, Url = pf.FullImageLink });

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
                int retry_count = 0;
                string temp_file_path = Path.Combine(temp_files_dir, f.Hash + f.Type.ToString());

                while (true)
                {
                    HttpWebRequest nc = (HttpWebRequest)(WebRequest.Create(url));

                    nc.UserAgent = user_agent;
                    nc.Referer = referer;

                    f.RetryCount = retry_count;

                    if (retry_count >= 35)
                    {
                        f.Log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Fail,
                            Message = string.Format("Failed to download file '{0}', exceeded retry count of 35", f.Url),
                            Sender = "FileDumper",
                            Title = "-"
                        });
                        f.Status = FileQueueStateInfo.DownloadStatus.Error;
                        break;
                    }

                    int downloaded = 0;

                    if (File.Exists(temp_file_path))
                    {
                        FileInfo fifo = new FileInfo(temp_file_path);
                        if (fifo.Length > 0)
                        {
                            downloaded = Convert.ToInt32(fifo.Length);

                            if (nc.Headers[HttpRequestHeader.Range] != null)
                            {
                                nc.Headers.Remove(HttpRequestHeader.Range);
                            }

                            nc.AddRange(downloaded);

                            f.Log(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Info,
                                Message = string.Format("Resuming file {0} download from offset {1}", f.Url, downloaded),
                                Sender = "FileDumper",
                                Title = "-"
                            });

                        }
                    }

                    try
                    {
                        using (WebResponse wr = nc.GetResponse())
                        {

                            string content_length = wr.Headers[HttpResponseHeader.ContentLength];

                            double total_length = Convert.ToDouble(content_length);

                            f.Length = total_length;
                            //byte size
                            int b_s = 0;

                            byte[] buffer = new byte[2048];

                            using (FileStream fs = new FileStream(temp_file_path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                            {
                                if (downloaded > 0)
                                {
                                    fs.Seek(fs.Length, SeekOrigin.Begin);
                                }

                                using (Stream s = wr.GetResponseStream())
                                {
                                    f.Status = FileQueueStateInfo.DownloadStatus.Downloading;
                                    while ((b_s = s.Read(buffer, 0, 2048)) > 0)
                                    {
                                        fs.Write(buffer, 0, b_s);
                                        f.Downloaded += Convert.ToDouble(b_s);
                                        if (f.Type == FileQueueStateInfo.FileType.Thumbnail) { NetworkUsageCounter.ThumbConsumed += b_s; }
                                        if (f.Type == FileQueueStateInfo.FileType.FullFile) { NetworkUsageCounter.FileConsumed += b_s; }
                                    }
                                }//web response stream block
                            }// temporary file stream block
                        }//web response block

                        if (File.Exists(temp_file_path))
                        {
                            File.Move(temp_file_path, save_path);
                            f.Log(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Success,
                                Message = string.Format("Downloaded file '{0}' successfully", f.Url),
                                Sender = "FileDumper",
                                Title = "-"
                            });
                            f.Status = FileQueueStateInfo.DownloadStatus.Complete;
                        }
                        else
                        {
                            f.Log(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Fail,
                                Message = string.Format("Could not download the file '{0}' because temporary file does not exist", f.Url),
                                Sender = "FileDumper",
                                Title = "-"
                            });
                            f.Status = FileQueueStateInfo.DownloadStatus.Error;
                        }//temporary file block

                        break;
                    }
                    catch (Exception ex)
                    {
                        f.Log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Warning,
                            Message = string.Format("Error occured while downloading file '{0}': {1} @ {2}", f.Url, ex.Message, ex.StackTrace),
                            Sender = "FileDumper",
                            Title = "-"
                        });

                        System.Threading.Thread.Sleep(1500);

                        retry_count++;
                    }//try block
                }//while block

                return;
            }//wget check block
        }

        public static FileQueueStateInfo get_file_state(string hash)
        {
            if (queued_files.ContainsKey(hash))
            {
                return queued_files[hash];
            }
            else
            {
                return null;
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

        private static void LogMessage(LogEntry entry)
        {
            if (verbose) { PrintLog(entry); }
            logs.Add(entry);
        }

        public static void PrintLog(LogEntry entry)
        {
            //[Level] Time (sender - title) : message
            Console.WriteLine("[{0}] {1} {2}: {3}", entry.Level.ToString(), entry.Time.ToShortDateString() + entry.Time.ToShortTimeString(), entry.Title == "-" ? string.Format("({0})", entry.Sender) : string.Format("({0} - {1})", entry.Sender, entry.Title), entry.Message);
        }


    }
}
