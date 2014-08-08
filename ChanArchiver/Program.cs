using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AniWrap.DataTypes;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace ChanArchiver
{
    class Program
    {
        public static string file_save_dir = "";
        public static string thumb_save_dir = "";
        public static string api_cache_dir = "";
        public static string post_files_dir = "";
        public static string temp_files_dir = "";
        public static string board_settings_dir = "";


        public static string program_dir;
        public static string ffmpeg_path;

        static string board = "";

        public static Amib.Threading.SmartThreadPool thumb_stp;
        public static Amib.Threading.SmartThreadPool file_stp;

        public static AniWrap.AniWrap aw;

        public static bool verbose = false;

        public static Dictionary<string, BoardWatcher> active_dumpers = new Dictionary<string, BoardWatcher>();

        public static Dictionary<string, FileQueueStateInfo> queued_files = new Dictionary<string, FileQueueStateInfo>();

        public static DateTime StartUpTime = DateTime.Now;

        private static int port = 8787;

        public static KeyValuePair<string, string>[] ValidBoards { get; private set; }

        private static FileSystemWatcher swf_watch;

        static void Main(string[] args)
        {
            WebRequest.DefaultWebProxy = null;
            ServicePointManager.DefaultConnectionLimit = 1000;

            int single_id = -1;

            bool server = true;

            bool is_t = false;

            foreach (string arg in args)
            {
                if (arg.StartsWith("--thread:"))
                {
                    // --thread:board:id
                    board = arg.Split(':')[1];
                    single_id = Convert.ToInt32(arg.Split(':')[2]);
                }

                if (arg == "--noserver")
                {
                    server = false;
                }

                if (arg.StartsWith("--board:"))
                {
                    board = arg.Split(':')[1];
                }

                if (arg == "--thumbonly")
                {
                    is_t = true;
                }

                if (arg == "--verbose")
                {
                    verbose = true;
                }

                if (arg.StartsWith("--port:"))
                {
                    Int32.TryParse(arg.Split(':')[1], out port);
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

            thumb_stp = new Amib.Threading.SmartThreadPool() { MaxThreads = 10, MinThreads = 0 };
            thumb_stp.Start();

            file_stp = new Amib.Threading.SmartThreadPool() { MaxThreads = 5, MinThreads = 0 };
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
            board_settings_dir = Path.Combine(program_dir, "settings");

            Directory.CreateDirectory(file_save_dir);
            Directory.CreateDirectory(thumb_save_dir);
            Directory.CreateDirectory(post_files_dir);
            Directory.CreateDirectory(api_cache_dir);
            Directory.CreateDirectory(temp_files_dir);
            Directory.CreateDirectory(board_settings_dir);

            Settings.Load();

            if (is_t) { Settings.ThumbnailOnly = true; }

            aw = new AniWrap.AniWrap(api_cache_dir);

            Console.Title = "ChanArchiver";

            print("ChanArchiver", ConsoleColor.Cyan);
            Console.WriteLine(" v1.0 stable");


            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                string[] ffmpegs_paths = { get_ffmpeg_path_unix(), "/bin/ffmpeg", "/usr/bin/ffmpeg", };

                foreach (string s in ffmpegs_paths)
                {
                    if (File.Exists(s))
                    {
                        ffmpeg_path = s;
                        Console.Write("Detected ffmpeg path:");
                        print(string.Format("'{0}'\n", s), ConsoleColor.Yellow);
                        break;
                    }
                }
            }
            else
            {
                ffmpeg_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ffmpeg.exe");
                Console.WriteLine("Make sure you place ffmpeg.exe in your home folder");
            }

            swf_watch = new FileSystemWatcher(file_save_dir);
            swf_watch.EnableRaisingEvents = true;
            swf_watch.Filter = "*.swf";
            swf_watch.IncludeSubdirectories = false;
            swf_watch.Created += new FileSystemEventHandler(handle_new_swf_file);

            Console.Write("Downloading board data...");
            ValidBoards = aw.GetAvailableBoards();
            Console.Write("loaded {0} board.\n", ValidBoards.Length);

            Console.Write("Saving files in ");
            print(string.Format("'{0}'\n", program_dir), ConsoleColor.Red);

            load_settings();

            if (Settings.ThumbnailOnly)
            {
                print("Warning:", ConsoleColor.Yellow);
                Console.WriteLine("ChanArchiver is running in thumbnail only mode");
            }

            if (server)
            {
                Console.WriteLine("ChanArchiver is running in server mode");
                start_server();
            }

            if (!string.IsNullOrEmpty(board))
            {
                if (single_id > 0)
                {
                    archive_single(board, single_id, Settings.ThumbnailOnly);
                }
                else
                {
                    archive_board(board, BoardWatcher.BoardMode.FullBoard);
                }
            }

            Task.Factory.StartNew((Action)delegate { try { build_file_index(); } catch (Exception) { } });

            //Console.WriteLine("Building file index...");

            //try
            //{
            //    build_file_index();
            //    Stopwatch sw = new Stopwatch();
            //    sw.Start();
            //    build_file_index();
            //    sw.Stop();
            //    Console.WriteLine("Total {0} unique file have been processed in {1} seconds", file_index.Count, sw.Elapsed.TotalSeconds);
            //}
            //catch (Exception) { }



            interactive_console();

            save_settings();
            FileSystemStats.Dispose();
        }

        private static string get_ffmpeg_path_unix()
        {
            ProcessStartInfo p = new ProcessStartInfo("which");
            p.Arguments = "ffmpeg";
            p.RedirectStandardOutput = true;
            p.UseShellExecute = false;
            p.CreateNoWindow = true;
            using (Process proc = Process.Start(p))
            {
                proc.WaitForExit();
                return proc.StandardOutput.ReadToEnd().Trim();
            }
        }

        private static Dictionary<string, FileIndexInfo> file_index = new Dictionary<string, FileIndexInfo>();

        public class FileIndexInfo
        {
            public FileIndexInfo(string hash)
            {
                this.Hash = hash;
            }

            public string Hash { get; private set; }

            public struct Post
            {
                public string Board;
                public int ThreadID;
                public int PostID;
                public string FileName;
                //public string ToString()
                //{
                //    return string.Format("{0}-{1}-{2}", this.Board, this.ThreadID, this.PostID);
                //}
            }

            private string get_post_hash(string board, int threadid, int postid)
            {
                return string.Format("{0}-{1}-{2}", board, threadid, postid);
            }

            private Dictionary<string, Post> my_posts = new Dictionary<string, Post>();

            public void MarkPost(string board, int threadid, int postid, string file_name)
            {
                string hash = get_post_hash(board, threadid, postid);
                if (!my_posts.ContainsKey(hash))
                {
                    my_posts.Add(hash, new Post()
                    {
                        Board = board,
                        ThreadID = threadid,
                        PostID = postid,
                        FileName = file_name
                    });
                }
            }

            public int RepostCount
            {
                get
                {
                    return my_posts.Count();
                }
            }

            public Post[] GetRepostsData() { return my_posts.Values.ToArray(); }
        }

        private static bool is_index_building = false;

        private static void build_file_index()
        {
            if (is_index_building) { return; }
            is_index_building = true;
            foreach (var board in ValidBoards)
            {
                var threads = ThreadStore.GetIndex(board.Key);

                if (threads.Length > 0)
                {
                    for (int t_index = 0; t_index < threads.Length; t_index++)
                    {
                        var thread_data = ThreadStore.GetThread(board.Key, threads[t_index].PostID.ToString());

                        foreach (var post in thread_data)
                        {
                            if (post.MyFile != null)
                            {
                                FileIndexInfo w;

                                if (file_index.ContainsKey(post.MyFile.Hash))
                                {
                                    w = file_index[post.MyFile.Hash];
                                }
                                else
                                {
                                    w = new FileIndexInfo(post.MyFile.Hash);
                                    file_index.Add(post.MyFile.Hash, w);
                                }

                                w.MarkPost(board.Key, threads[t_index].PostID, post.PostID, post.MyFile.FileName);
                            }
                        }
                    }
                }
            }
            is_index_building = false;
        }

        public static void update_file_index()
        {
            file_index.Clear();
            GC.Collect();
            build_file_index();
        }

        public static FileIndexInfo get_file_index_state(string hash)
        {
            if (file_index.ContainsKey(hash)) { return file_index[hash]; }
            return null;
        }

        private static void interactive_console()
        {
            print("Interactive Console", ConsoleColor.Yellow);
            Console.Write(" has started.\nType (help) to view available commands or (exit) to quit\n");

            string command = Console.ReadLine().Trim().ToLower();

            while (command != "exit")
            {
                switch (command)
                {
                    case "save":
                        save_settings();
                        break;
                    case "optimize-all":
                        optimize_all_threads();
                        break;
                    case "toggle-ff":
                        Settings.ThumbnailOnly = !Settings.ThumbnailOnly;
                        Console.WriteLine("Full files saving is {0}", Settings.ThumbnailOnly ? "disabled" : "enabled");
                        break;
                    case "swf-gen":
                        {
                            FileInfo[] swfs = (new DirectoryInfo(file_save_dir)).GetFiles("*.swf");

                            foreach (FileInfo swf in swfs)
                            {
                                make_swf_thumb(swf.FullName);
                            }
                        }
                        break;
                    case "help":
                        Console.WriteLine("- help: view this text");
                        Console.WriteLine("- save: save settings");
                        Console.WriteLine("- optimize-all: optimize all non-active threads");
                        Console.WriteLine("- swf-gen: generate thumbnails for .swf files using ffmpeg");
                        Console.WriteLine("- exit: save settings and exit the program");
                        Console.WriteLine("- toggle-ff: Enable or disable full file saving.");
                        Console.WriteLine("- optimize-gif: Convert all gifs files to .webm to save disk space. This action is not reversible");
                        break;
                    case "bench":
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        build_file_index();
                        sw.Stop();
                        Console.WriteLine("Total {0} unique file have been processed in {1} seconds", file_index.Count, sw.Elapsed.TotalSeconds);
                        break;
                    case "sanitize-files":
                        {

                            update_file_index();

                            var files = Directory.EnumerateFiles(file_save_dir);
                            int i = 0;
                            foreach (var file in files)
                            {
                                string file_hash = file.Split('\\').Last().Split('.').First();
                                if (!file_index.ContainsKey(file_hash))
                                {
                                    i++;
                                    File.Delete(file);
                                }
                            }
                            Console.WriteLine("Removed {0} file", i);

                            update_file_index();
                            break;
                        }
                    case "optimize-gif":
                        convert_all_gifs();
                        break;
                    case "":
                        //Console.WriteLine(":^)");
                        break;
                    default:
                        Console.WriteLine("Unkown command '{0}'", command);
                        break;
                }
                command = Console.ReadLine().Trim().ToLower();
            }
        }

        private static void convert_all_gifs()
        {
            if (File.Exists(ffmpeg_path))
            {
                string temp = Path.Combine(temp_files_dir, "gif2webm");
                Directory.CreateDirectory(temp);

                DirectoryInfo in_info = new DirectoryInfo(file_save_dir);

                FileInfo[] files = in_info.GetFiles("*.gif", SearchOption.TopDirectoryOnly);
                Console.WriteLine("Total GIFS found: {0}", files.Length);
                int kb = 200 * 1024;

                double gif_processed = 0;
                double webm_produced = 0;

                for (int index = 0; index < files.Length; index++)
                {
                    FileInfo fifo = files[index];

                    if (fifo.Length > kb)
                    {
                        string webm_n = fifo.Name + ".webm";
                        string webm_name = Path.Combine(temp, webm_n);

                        string webm_gif_path = Path.Combine(fifo.Directory.FullName, webm_n);

                        if (File.Exists(webm_gif_path))
                        {
                            Console.Beep();
                            Console.WriteLine("PANIC: Duplicate WEBM/GIF: {0}. Stopping", fifo.Name);
                            return;
                        }

                        try { convert_to_webm(fifo.FullName, webm_name); }
                        catch { }

                        if (File.Exists(webm_name))
                        {
                            FileInfo webm_info = new FileInfo(webm_name);

                            webm_produced += webm_info.Length;
                            gif_processed += fifo.Length;

                            File.Delete(fifo.FullName);

                            File.Move(webm_name, webm_gif_path);

                            Console.Write(string.Format("{0}/{1}\n", index + 1, files.Length));
                        }
                        else
                        {
                            Console.WriteLine("Failed to convert file {0}", fifo.FullName);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Not converting {0}, file size is less than 200KB", fifo.Name);
                    }

                }
                Directory.Delete(temp, true);

                Console.WriteLine("Total processed GIFs size: {0}", format_size_string(gif_processed));

                Console.WriteLine("Total WebM produced size: {0}", format_size_string(webm_produced));

                Console.WriteLine("Disk space saved: {0} ( {1} % )", format_size_string(gif_processed - webm_produced), 100 - ((webm_produced / gif_processed) * 100));
            }
        }

        private static void convert_to_webm(string file, string output)
        {
            ProcessStartInfo psr = new ProcessStartInfo(ffmpeg_path);

            psr.CreateNoWindow = true;
            psr.UseShellExecute = false;

            psr.Arguments = string.Format("-i \"{0}\" -c:v libvpx -crf 12 -b:v 500K \"{1}\"", file, output);

            using (Process proc = Process.Start(psr))
            {
                proc.WaitForExit();
            }
            return;
        }

        private static void optimize_all_threads()
        {
            DirectoryInfo[] boards = new DirectoryInfo(post_files_dir).GetDirectories();
            foreach (DirectoryInfo board in boards)
            {
                DirectoryInfo[] threads = board.GetDirectories();

                foreach (DirectoryInfo thread in threads)
                {
                    if (active_dumpers.ContainsKey(board.Name))
                    {
                        BoardWatcher bw = active_dumpers[board.Name];

                        int w = -1;
                        Int32.TryParse(thread.Name, out w);
                        if (bw.watched_threads.ContainsKey(w))
                        {
                            continue;
                        }
                    }
                    ThreadWorker.optimize_thread_file(thread.FullName);
                    Console.WriteLine("Optimized thread {0} - {1}", board.Name, thread.Name);
                }
            }
            Console.WriteLine("Done");
        }

        private static void load_settings()
        {
            Console.WriteLine("Loading banned files list...");
            load_banned_files_list();

            Console.WriteLine("Loading network statistics...");
            NetworkUsageCounter.LoadStats();

            Console.WriteLine("Loading manually added threads...");
            load_boards();
        }

        private static void save_settings()
        {
            Console.WriteLine("Saving network statistics...");
            NetworkUsageCounter.SaveStats();

            Console.WriteLine("Saving manually added threads...");
            save_boards();

            Console.WriteLine("Saving banned files list...");
            save_banned_files_list();

            Settings.Save();
        }

        private static void save_boards()
        {
            List<string> boards = new List<string>(active_dumpers.Count);

            foreach (KeyValuePair<string, BoardWatcher> bw in active_dumpers)
            {
                bw.Value.SaveManuallyAddedThreads();
                bw.Value.SaveFilters();
                boards.Add(string.Format("{0}:{1}", bw.Key, Convert.ToInt32(bw.Value.Mode)));
            }

            File.WriteAllLines(added_boards_save_file_path, boards.ToArray());
        }

        private static string added_boards_save_file_path
        {
            get { return Path.Combine(board_settings_dir, "added-boards.txt"); }
        }

        private static void load_boards()
        {
            if (File.Exists(added_boards_save_file_path))
            {
                foreach (string s in File.ReadAllLines(added_boards_save_file_path))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(s))
                        {
                            string[] data = s.Split(':');
                            if (data.Length == 2)
                            {
                                archive_board(data[0], (BoardWatcher.BoardMode)Convert.ToInt32(data[1]));
                            }
                            else if (data.Length == 1)
                            {
                                archive_board(data[0], BoardWatcher.BoardMode.None);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
        }

        #region SWF Thumbnail Generator

        static void handle_new_swf_file(object sender, FileSystemEventArgs e)
        {
            //using ffmpeg, I try to generate a thumbnail for .swf files
            make_swf_thumb(e.FullPath);
        }

        private static void make_swf_thumb(string swf_path)
        {
            try
            {
                if (File.Exists(ffmpeg_path))
                {
                    FileInfo swf_file = new FileInfo(swf_path);
                    if (swf_file.Exists)
                    {
                        string output_file = Path.Combine(thumb_save_dir, swf_file.Name.Split('.')[0] + ".jpg");
                        if (!File.Exists(output_file))
                        {
                            System.Diagnostics.ProcessStartInfo psr = new System.Diagnostics.ProcessStartInfo(ffmpeg_path);
                            psr.CreateNoWindow = true;
                            psr.UseShellExecute = false;
                            psr.Arguments = string.Format("-i \"{0}\" -r 1 -t 2 -vframes 1 \"{1}\"", swf_file.FullName, output_file);
                            using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(psr))
                            {
                                p.WaitForExit();
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        #endregion

        private static void print(string text, ConsoleColor color)
        {
            ConsoleColor old_c = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = old_c;
        }

        public static void archive_single(string board, int id, bool thumbOnly)
        {
            if (active_dumpers.ContainsKey(board))
            {
                BoardWatcher bw = active_dumpers[board];
                bw.AddThreadId(id, thumbOnly);
            }
            else
            {
                BoardWatcher bw = new BoardWatcher(board);
                active_dumpers.Add(board, bw);
                bw.AddThreadId(id, thumbOnly);
            }
        }

        public static void archive_board(string board, BoardWatcher.BoardMode mode)
        {
            if (active_dumpers.ContainsKey(board))
            {
                BoardWatcher bw = active_dumpers[board];
                bw.StartMonitoring(mode);
            }
            else
            {
                BoardWatcher bw = new BoardWatcher(board);
                active_dumpers.Add(board, bw);
                bw.StartMonitoring(mode);
            }
        }

        private static void start_server()
        {
            try
            {
                if (Settings.EnableFileStats)
                {
                    FileSystemStats.Init();
                }

                HttpServer.HttpServer server = new HttpServer.HttpServer();

                server.ServerName = "ChanArchiver";

                server.Add(new ChanArchiver.HttpServerHandlers.OverviewPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.LogPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileQueuePageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.WatchJobsPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.MonitoredBoardsPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.ThreadFiltersPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileInfoPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.ResourcesHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.FileHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.BannedFilesPageHandler());
                server.Add(new ChanArchiver.HttpServerHandlers.SettingsPageHandler());


                server.Add(new ThreadServerModule());
                Console.WriteLine("Starting HTTP server...");
                server.Start(IPAddress.Any, port);
                Console.WriteLine("Listening on port {0}.\nWebsite is accessible at (http://*:{0})", port);
            }
            catch (Exception ex)
            {
                Console.Beep();
                Console.WriteLine("Could not start HTTP server: {0}.", ex.Message);
            }
        }

        #region File Downloader Logic

        public static void dump_files(PostFile pf, bool thumbonly)
        {
            string md5 = base64tostring(pf.hash);
            string file_path = Path.Combine(file_save_dir, md5 + "." + pf.ext);
            string thumb_path = Path.Combine(thumb_save_dir, md5 + ".jpg");

            string reff = string.Format("http://boards.4chan.org/{0}/res/{1}", pf.board, pf.owner);

            if (pf.ThumbLink != PostFile.NoFile)
            {
                if (!File.Exists(thumb_path))
                {
                    string thumb_key = "thumb" + md5;

                    if (!queued_files.ContainsKey(thumb_key))
                    {
                        FileQueueStateInfo f = new FileQueueStateInfo(md5, pf);
                        f.Type = FileQueueStateInfo.FileType.Thumbnail;
                        f.Url = pf.ThumbLink;

                        queued_files.Add(thumb_key, f);

                        thumb_stp.QueueWorkItem(new Amib.Threading.Action(delegate
                        {
                            download_file(thumb_path, reff, f);
                        }));
                    }
                }
            }

            if (!thumbonly)
            {
                if (!is_file_banned(md5))
                {
                    bool file_no_exist = !File.Exists(file_path);

                    if (pf.ext == "gif")
                    {
                        //This file was a gif that were converted to webm
                        //don't download it again
                        if (File.Exists(file_path + ".webm"))
                        {
                            file_no_exist = false;
                        }
                    }

                    if (file_no_exist) // GIF to webm check
                    {
                        string file_key = "file" + md5;
                        if (!queued_files.ContainsKey(file_key))
                        {
                            FileQueueStateInfo f = new FileQueueStateInfo(md5, pf);
                            f.Type = FileQueueStateInfo.FileType.FullFile;
                            f.Url = pf.FullImageLink;

                            queued_files.Add(file_key, f);

                            file_stp.QueueWorkItem(new Amib.Threading.Action(delegate
                            {
                                download_file(file_path, reff, f);
                            }), get_file_priority(pf));
                        }
                    }
                }
            }
        }

        private static Amib.Threading.WorkItemPriority get_file_priority(PostFile pf)
        {
            //smaller than 50KB, Highest Priority
            //between 50KB and 400KB, High pr
            //between 400KB and 1MB, normal pr
            //larger than 1MB, low
            //larger than 3mb, lowest
            int kb = 1024;
            int mb = 1048576;

            if (pf.size < 50 * kb)
            {
                return Amib.Threading.WorkItemPriority.Highest;
            }
            else if (pf.size > 50 * kb && pf.size < 400 * kb)
            {
                return Amib.Threading.WorkItemPriority.AboveNormal;
            }
            else if (pf.size >= 400 * kb && pf.size <= 1 * mb)
            {
                return Amib.Threading.WorkItemPriority.Normal;
            }
            else if (pf.size > 1 * mb && pf.size < 3 * mb)
            {
                return Amib.Threading.WorkItemPriority.BelowNormal;
            }
            else if (pf.size >= 3 * mb)
            {
                return Amib.Threading.WorkItemPriority.Lowest;
            }
            else
            {
                return Amib.Threading.WorkItemPriority.Normal;
            }
        }

        const string user_agent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:26.0) Gecko/20100101 Firefox/26.0";

        private static void download_file(string save_path, string referer, FileQueueStateInfo f)
        {
            f.Status = FileQueueStateInfo.DownloadStatus.Connecting;

            string temp_file_path = Path.Combine(temp_files_dir, f.Hash + f.Type.ToString());

            while (true)
            {
                HttpWebRequest nc = (HttpWebRequest)(WebRequest.Create(f.Url));

                nc.UserAgent = user_agent;
                nc.Referer = referer;

                if (f.RetryCount > 35)
                {
                    f.Log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Fail,
                        Message = "Failed to download the file, exceeded retry count of 35",
                        Title = "-"
                    });
                    f.Status = FileQueueStateInfo.DownloadStatus.Error;
                    break;
                }

                if (f.ForceStop)
                {
                    f.Log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Success,
                        Message = "File download stopped by user",
                        Sender = "FileDumper",
                        Title = "-"
                    });
                    if (File.Exists(temp_file_path)) { File.Delete(temp_file_path); }
                    f.Status = FileQueueStateInfo.DownloadStatus.Stopped;
                    return;
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
                            Message = string.Format("Resuming file download from offset {0}", downloaded),
                            Sender = "FileDumper",
                            Title = "-"
                        });

                        f.Downloaded = downloaded;
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
                                    if (f.ForceStop)
                                    {
                                        f.Log(new LogEntry()
                                        {
                                            Level = LogEntry.LogLevel.Success,
                                            Message = "File download stopped by user",
                                            Sender = "FileDumper",
                                            Title = "-"
                                        });

                                        fs.Close();
                                        File.Delete(temp_file_path);
                                        f.Status = FileQueueStateInfo.DownloadStatus.Stopped;
                                        return;
                                    }

                                    fs.Write(buffer, 0, b_s);

                                    f.Downloaded += Convert.ToDouble(b_s);

                                    if (f.Type == FileQueueStateInfo.FileType.Thumbnail)
                                    { NetworkUsageCounter.Add_ThumbConsumed(b_s); }
                                    else
                                    { NetworkUsageCounter.Add_FileConsumed(b_s); }

                                }
                            }//web response stream block
                        }// temporary file stream block
                    }//web response block

                    if (File.Exists(temp_file_path))
                    {
                        //don't check hashes for thumbnails
                        if (f.Type == FileQueueStateInfo.FileType.Thumbnail || verify_file_checksums(temp_file_path, f.Hash))
                        {
                            File.Move(temp_file_path, save_path);

                            f.Log(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Success,
                                Message = "Downloaded file successfully",
                                Sender = "FileDumper",
                                Title = "-"
                            });

                            f.Status = FileQueueStateInfo.DownloadStatus.Complete;

                            if (Settings.AutoRemoveCompleteFiles)
                            {
                                queued_files.Remove((f.Type == FileQueueStateInfo.FileType.FullFile ? "file" : "thumb") + f.Hash);
                            }

                        }
                        else
                        {
                            f.Log(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Warning,
                                Message = string.Format("Downloaded file was corrupted, retrying", f.Url),
                                Sender = "FileDumper",
                                Title = "-"
                            });
                            f.Downloaded = 0;
                            File.Delete(temp_file_path);
                            f.RetryCount++;
                            continue;
                        }
                    }
                    else
                    {
                        f.Log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Fail,
                            Message = "Could not download the file because temporary file does not exist",
                            Sender = "FileDumper",
                            Title = "-"
                        });
                        f.Status = FileQueueStateInfo.DownloadStatus.Error;
                    }//temporary file block

                    break;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        f.Log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Fail,
                            Message = "Cannot download the file, server returned HTTP 404 Not Found.",
                            Sender = "FileDumper",
                            Title = "-"
                        });
                        f.Status = FileQueueStateInfo.DownloadStatus.NotFound;
                        return;
                    }
                    else
                    {
                        f.Log(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Warning,
                                Message = string.Format("Error occured while downloading the file: {0} @ {1}", ex.Message, ex.StackTrace),
                                Sender = "FileDumper",
                                Title = "-"
                            });
                    }
                    System.Threading.Thread.Sleep(1000);
                    f.RetryCount++;
                }//try block
            }//while block

            return;
        }

        #endregion

        #region Banned Files Sub-system

        static List<string> banned_hashes;

        public static bool is_file_banned(string hash)
        {
            return banned_hashes.Contains(hash);
        }

        public static void ban_file(string hash)
        {
            if (!banned_hashes.Contains(hash)) { banned_hashes.Add(hash); save_banned_files_list(); }
        }

        public static void unban_file(string hash)
        {
            banned_hashes.Remove(hash); save_banned_files_list();
        }

        public static string[] get_banned_file_list()
        {
            if (banned_hashes != null)
            {
                return banned_hashes.ToArray();
            }

            return new string[0];
        }

        private static string banned_files_savepath
        {
            get { return Path.Combine(board_settings_dir, "banned-files.json"); }
        }

        private static void save_banned_files_list()
        {
            File.WriteAllText(banned_files_savepath, Newtonsoft.Json.JsonConvert.SerializeObject(banned_hashes));
        }

        private static void load_banned_files_list()
        {
            if (File.Exists(banned_files_savepath))
            {
                Newtonsoft.Json.Linq.JArray arr = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JArray>(File.ReadAllText(banned_files_savepath));

                banned_hashes = new List<string>(arr.Count);

                foreach (Newtonsoft.Json.Linq.JValue obj in arr)
                {
                    banned_hashes.Add(Convert.ToString(obj));
                }
            }
            else { banned_hashes = new List<string>(); }
        }

        #endregion

        #region Miscellaneous Functions

        private static bool verify_file_checksums(string path, string md5_hash)
        {
            string computed_hash = "";

            using (System.Security.Cryptography.MD5CryptoServiceProvider md = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                StringBuilder sb = new StringBuilder();
                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open))
                    {
                        byte[] hash = md.ComputeHash(fs);

                        foreach (byte b in hash)
                        {
                            sb.Append(b.ToString("X2"));
                        }
                        computed_hash = sb.ToString().ToLower();
                    }

                }
                catch (Exception)
                {
                    //probably the file does not exist
                    //this return statement should NEVER occure, since the calling function check for file existance 
                    //before calling, however, I don't want any execption throwing.
                    return false;
                }
            }

            return md5_hash.ToLower() == computed_hash;
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
            else if (size > KB && size < MB)
            {
                return Math.Round(size / KB, 2).ToString() + " KB";
            }
            else if (size > MB && size < GB)
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

        #endregion

        #region Logging

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

        #endregion


    }
}
