using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver
{
    public class ThreadServerModule : HttpServer.HttpModules.HttpModule
    {
        private const int ThreadPerPage = 10;

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command.StartsWith("/boards/"))
            {
                response.Encoding = System.Text.Encoding.UTF8;

                string[] parame = command.Split('?')[0].Split('/');

                if (parame.Length == 3)
                {
                    //board index view mode
                    string board = parame[2];
                    if (string.IsNullOrEmpty(board))
                    {
                        _404(response);
                        return true;
                    }
                    else
                    {
                        string board_folder = Path.Combine(Program.post_files_dir, board);

                        if (Directory.Exists(board_folder))
                        {
                            DirectoryInfo info = new DirectoryInfo(board_folder);

                            DirectoryInfo[] folders = info.GetDirectories();

                            int thread_count = 0;

                            Dictionary<string, string> threads = new Dictionary<string, string>();

                            for (int i = 0; i < folders.Count(); i++)
                            {
                                string op_file = Path.Combine(folders[i].FullName, "op.json");
                                string optimized = Path.Combine(folders[i].FullName, folders[i].Name + "-opt.json");

                                if (File.Exists(op_file))
                                {
                                    thread_count++;
                                    threads.Add(folders[i].Name, File.ReadAllText(op_file));

                                }
                                else if (File.Exists(optimized))
                                {
                                    Dictionary<string, object> t = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(optimized));
                                    if (t.ContainsKey("op"))
                                    {
                                        thread_count++;
                                        threads.Add(folders[i].Name, t["op"].ToString());
                                    }
                                }
                                else
                                {
                                    if (Program.active_dumpers.ContainsKey(board_folder))
                                    {
                                        BoardWatcher bw = Program.active_dumpers[board_folder];
                                        int tid = -1;
                                        Int32.TryParse(folders[i].Name, out tid);
                                        if (tid > 0)
                                        {
                                            if (!bw.watched_threads.ContainsKey(tid))
                                            {
                                                Directory.Delete(folders[i].FullName);
                                            }
                                        }

                                    }
                                }
                            }

                            int page_count = (int)Math.Round(Convert.ToDouble(thread_count / ThreadPerPage), MidpointRounding.AwayFromZero);

                            if (page_count <= 0) { page_count = 1; }

                            int page_offset = 0;

                            Int32.TryParse(request.QueryString["pn"].Value, out page_offset);

                            page_offset = Math.Abs(page_offset);

                            StringBuilder s = new StringBuilder();

                            int start = page_offset * (ThreadPerPage - 1);
                            int end = start + ThreadPerPage;

                            for (int i = start; i < end && i < thread_count; i++)
                            {
                                s.Append("<div class='row'>");
                                s.Append
                                    (
                                          load_post_data_str(threads.ElementAt(i).Value, true).ToString()
                                          .Replace("{op:replycount}", "")
                                          .Replace("{postLink}", string.Format("/boards/{0}/{1}", board, threads.ElementAt(i).Key))
                                    );

                                s.Append("</div>");
                            }

                            StringBuilder page_numbers = new StringBuilder();

                            for (int i = 0; i < page_count + 3; i++)
                            {
                                if (i == page_offset)
                                {
                                    page_numbers.AppendFormat("<li class=\"active\"><a href=\"?pn={0}\">{1}</a></li>", i, i + 1);
                                }
                                else
                                {
                                    page_numbers.AppendFormat("<li><a href=\"?pn={0}\">{1}</a></li>", i, i + 1);
                                }
                            }

                            byte[] data = System.Text.Encoding.UTF8.GetBytes(
                                Properties.Resources.board_index_page
                                .Replace("{po}", Convert.ToString(page_offset - 1))
                                .Replace("{no}", Convert.ToString(page_offset + 1))
                                .Replace("{pagen}", page_numbers.ToString())
                                .Replace("{Items}", s.ToString()));

                            response.ContentType = "text/html";
                            response.Status = System.Net.HttpStatusCode.OK;
                            response.ContentLength = data.Length;
                            response.SendHeaders();
                            response.SendBody(data);
                        }
                        else
                        {
                            _404(response);
                        }

                        return true;
                    }
                }
                else if (parame.Length == 4)
                {
                    //thread view mode
                    string board = parame[2];
                    string threadid = parame[3];

                    if (string.IsNullOrEmpty(board) || string.IsNullOrEmpty(threadid))
                    {
                        _404(response);
                    }
                    else
                    {
                        string thread_folder_path = Path.Combine(Program.post_files_dir, board, threadid);

                        if (Directory.Exists(thread_folder_path))
                        {
                            StringBuilder body = new StringBuilder();

                            body.AppendFormat("<div class=\"thread\" id=\"t{0}\">", threadid);

                            string opt_path = Path.Combine(thread_folder_path, threadid + "-opt.json");

                            if (File.Exists(opt_path))
                            {
                                Dictionary<string, object> thread_data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(opt_path));

                                body.Append(load_post_data_str(thread_data["op"].ToString(), true).ToString());

                                body.Replace("{op:replycount}", Convert.ToString(thread_data.Count() - 1));

                                thread_data.Remove("op");

                                IOrderedEnumerable<string> sorted_keys = thread_data.Keys.OrderBy(x => Convert.ToInt32(x));

                                foreach (string key in sorted_keys)
                                {
                                    body.Append(load_post_data_str(thread_data[key].ToString(), false).ToString());
                                }

                            }
                            else
                            {
                                DirectoryInfo info = new DirectoryInfo(thread_folder_path);

                                FileInfo[] files = info.GetFiles("*.json", SearchOption.TopDirectoryOnly);

                                body.Append(load_post_data(new FileInfo(Path.Combine(thread_folder_path, "op.json")), true).ToString());

                                body.Replace("{op:replycount}", Convert.ToString(files.Count() - 1));

                                IOrderedEnumerable<FileInfo> sorted = files.OrderBy(x => x.Name);

                                int cou = sorted.Count();

                                for (int i = 0; i < cou - 1; i++)
                                {
                                    body.Append(load_post_data(sorted.ElementAt(i), false));
                                }
                            }


                            body.Append("</div>");

                            byte[] respon = System.Text.Encoding.UTF8.GetBytes
                                (Properties.Resources.full_page
                                .Replace("{board}", board)
                                .Replace("{tid}", threadid)
                                .Replace("{DocumentBody}", body.ToString()));

                            response.ContentLength = respon.Length;

                            response.SendHeaders();
                            response.SendBody(respon);
                        }
                        else
                        {
                            _404(response);
                        }

                        return true;
                    }
                }
                else
                {
                    _404(response);
                }
            }

            if (command.StartsWith("/getfilelist?"))
            {
                int tid = -1;
                Int32.TryParse(request.QueryString["thread"].Value, out tid);

                string board = request.QueryString["board"].Value;

                if (tid > 0 && !string.IsNullOrEmpty(board))
                {
                    string board_folder = Path.Combine(Program.post_files_dir, board);

                    if (Directory.Exists(board_folder))
                    {
                        string thread_folder = Path.Combine(board_folder, tid.ToString());

                        if (Directory.Exists(thread_folder))
                        {

                            DirectoryInfo thread_folder_info = new DirectoryInfo(thread_folder);

                            string opt_path = Path.Combine(thread_folder, thread_folder_info.Name + "-opt.json");

                            StringBuilder sb = new StringBuilder();

                            if (File.Exists(opt_path))
                            {
                                Dictionary<string, object> thread_data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(opt_path));
                                foreach (object s in thread_data.Values)
                                {
                                    PostFormatter pf = load_post_data_str(s.ToString(), false);
                                    if (pf.MyFile != null)
                                    {
                                        string url_name = System.Web.HttpUtility.UrlEncodeUnicode(pf.MyFile.FileName);
                                        string url = string.Format("/filecn/{0}.{1}?cn={2}", pf.MyFile.Hash, pf.MyFile.Extension, url_name);
                                        sb.AppendFormat("<a href='{0}'>{1}</a><br/>", url, pf.MyFile.FileName);
                                    }
                                }
                            }
                            else
                            {
                                foreach (FileInfo f in thread_folder_info.GetFiles("*.json", SearchOption.TopDirectoryOnly))
                                {
                                    PostFormatter pf = load_post_data(f, false);
                                    if (pf.MyFile != null)
                                    {
                                        string url_name = System.Web.HttpUtility.UrlEncodeUnicode(pf.MyFile.FileName);
                                        string url = string.Format("/filecn/{0}.{1}?cn={2}", pf.MyFile.Hash, pf.MyFile.Extension, url_name);
                                        sb.AppendFormat("<a href='{0}'>{1}</a><br/>", url, pf.MyFile.FileName);
                                    }
                                }
                            }

                            response.Encoding = Encoding.UTF8;

                            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
                            response.ContentType = "text/html";
                            response.ContentLength = data.Length;
                            response.SendHeaders();
                            response.SendBody(data);
                            return true;
                        }
                    }

                    return false;
                }
                else
                {
                    _404(response);
                    return true;
                }
            }

            if (command == "/boards" || command == "/boards/")
            {
                response.Encoding = System.Text.Encoding.UTF8;

                if (Directory.Exists(Program.post_files_dir))
                {
                    response.ContentType = "text/html";
                    response.Status = System.Net.HttpStatusCode.OK;

                    DirectoryInfo info = new DirectoryInfo(Program.post_files_dir);

                    DirectoryInfo[] folders = info.GetDirectories();

                    StringBuilder s = new StringBuilder();

                    for (int i = 0; i < folders.Length; i++)
                    {
                        s.Append("<div class=\"col-6 col-sm-6 col-lg-4\">");

                        s.AppendFormat("<h2>/{0}/</h2>", folders[i].Name);
                        s.AppendFormat("<p>Thread Count: {0}</p>", folders[i].GetDirectories().Count());

                        s.AppendFormat("<p><a class=\"btn btn-default\" href=\"/boards/{0}\" role=\"button\">browse »</a></p>", folders[i].Name);

                        s.Append("</div>");

                        // s.AppendFormat("<a href='/boards/{0}'>/{0}/</a><br/>", folders[i].Name);
                    }

                    byte[] data = System.Text.Encoding.UTF8.GetBytes(Properties.Resources.archivedboard_page.Replace("{Items}", s.ToString()));

                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);

                }
                else
                {
                    _404(response);
                }

                return true;
            }

            if (command.StartsWith("/set/maxfilequeue/"))
            {
                if (string.IsNullOrEmpty(request.QueryString["count"].Value))
                {
                    _404(response);
                }
                else
                {
                    int t = Program.file_stp.MaxThreads;

                    Int32.TryParse(request.QueryString["count"].Value, out t);

                    if (t != Program.file_stp.MaxThreads)
                    {
                        if (t > Program.file_stp.MinThreads)
                        {
                            Program.file_stp.MaxThreads = t;
                        }
                    }
                    response.Redirect("/fq");
                }
                return true;
            }

            if (command.StartsWith("/add/"))
            {
                string[] rdata = command.Split('/');
                string mode = rdata[2].ToLower();

                if (mode == "board")
                {
                    if (string.IsNullOrEmpty(request.QueryString["boardletter"].Value))
                    {
                        _404(response);
                    }

                    string board = request.QueryString["boardletter"].Value;
                    string mon_type = request.QueryString["montype"].Value;

                    BoardWatcher.BoardMode m = BoardWatcher.BoardMode.None;

                    if (mon_type == "part") { m = BoardWatcher.BoardMode.Monitor; }
                    if (mon_type == "full") { m = BoardWatcher.BoardMode.FullBoard; }

                    Program.archive_board(board, m);

                    response.Status = System.Net.HttpStatusCode.OK;

                    response.Redirect("/monboards");

                }
                else if (mode == "thread")
                {
                    if (string.IsNullOrEmpty(request.QueryString["urlorformat"].Value))
                    {
                        _404(response);
                    }

                    string input = request.QueryString["urlorformat"].Value;

                    string board = "";
                    int id = -1;


                    if (input.ToLower().StartsWith("http"))
                    {
                        //http://boards.4chan.org/g/res/39075359
                        string temp = input.ToLower().Replace("https://", "").Replace("http://", "");

                        //boards.4chan.org/g/res/int
                        // 0               1  2  3 
                        string[] data = temp.Split('/');

                        if (data.Length >= 4)
                        {
                            board = data[1];

                            Int32.TryParse(data[3].Split('#')[0], out id);
                        }
                    }
                    else
                    {
                        string[] data = input.Split(':');
                        if (data.Length >= 2)
                        {
                            board = data[0];
                            Int32.TryParse(data[1], out id);
                        }
                    }


                    if (id > 0 & !string.IsNullOrEmpty(board))
                    {
                        Program.archive_single(board, id);
                        response.Status = System.Net.HttpStatusCode.OK;

                        response.Redirect("/wjobs");
                    }
                    else
                    {
                        _404(response);
                    }
                }
                else
                {
                    _404(response);
                }

                return true;
            }

            if (command.StartsWith("/cancel/"))
            {
                string[] data = command.Split('/');
                string mode = data[2];

                if (mode == "bw")
                {
                    string board = data[3];
                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        bw.StopMonitoring();
                        response.Redirect("/monboards");
                    }

                }

                if (mode == "bwr")
                {
                    string board = data[3];
                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        bw.StartMonitoring(BoardWatcher.BoardMode.FullBoard);
                        response.Redirect("/monboards");
                    }
                }

                if (mode == "tw")
                {
                    string board = data[3];
                    string tid = data[4];

                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        int id = Convert.ToInt32(tid);
                        if (bw.watched_threads.ContainsKey(id))
                        {
                            ThreadWorker tw = bw.watched_threads[id];
                            tw.Stop();
                            response.Redirect("/wjobs");
                        }
                    }
                }

                if (mode == "twr")
                {
                    string board = data[3];
                    string tid = data[4];

                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        int id = Convert.ToInt32(tid);
                        if (bw.watched_threads.ContainsKey(id))
                        {
                            ThreadWorker tw = bw.watched_threads[id];
                            tw.Start();
                            response.Redirect("/wjobs");
                        }
                    }
                }

                return true;
            }

            if (command == "/action/removecompletefiles")
            {
                List<string> hashes_to_remove = new List<string>();
                for (int index = 0; index < Program.queued_files.Count(); index++)
                {
                    try
                    {
                        FileQueueStateInfo f = Program.queued_files.ElementAt(index).Value;
                        if (f.Status == FileQueueStateInfo.DownloadStatus.Complete)
                        {
                            hashes_to_remove.Add(Program.queued_files.ElementAt(index).Key);
                        }
                    }
                    catch (Exception)
                    {
                        if (index > Program.queued_files.Count()) { break; }
                    }
                }

                foreach (string s in hashes_to_remove)
                {
                    Program.queued_files.Remove(s);
                }

                response.Redirect("/fq");

                return true;
            }

            if (command == "/action/removefailedfiles")
            {
                List<string> hashes_to_remove = new List<string>();
                for (int index = 0; index < Program.queued_files.Count(); index++)
                {
                    try
                    {
                        FileQueueStateInfo f = Program.queued_files.ElementAt(index).Value;
                        if (f.Status == FileQueueStateInfo.DownloadStatus.Error)
                        {
                            hashes_to_remove.Add(Program.queued_files.ElementAt(index).Key);
                        }
                    }
                    catch (Exception)
                    {
                        if (index > Program.queued_files.Count()) { break; }
                    }
                }

                foreach (string s in hashes_to_remove)
                {
                    Program.queued_files.Remove(s);
                }

                response.Redirect("/fq");

                return true;
            }

            if (command == "/action/restartfailedfiles")
            {
                List<KeyValuePair<string, FileQueueStateInfo>> files_to_restart = new List<KeyValuePair<string, FileQueueStateInfo>>();

                for (int index = 0; index < Program.queued_files.Count(); index++)
                {
                    try
                    {
                        FileQueueStateInfo f = Program.queued_files.ElementAt(index).Value;
                        if (f.Status == FileQueueStateInfo.DownloadStatus.Error)
                        {
                            files_to_restart.Add(Program.queued_files.ElementAt(index));
                        }
                    }
                    catch (Exception)
                    {
                        if (index > Program.queued_files.Count()) { break; }
                    }
                }

                foreach (KeyValuePair<string, FileQueueStateInfo> s in files_to_restart)
                {
                    Program.queued_files.Remove(s.Key);
                    Program.dump_files(s.Value.PostFile);
                }
                response.Redirect("/fq");

                return true;
            }

            if (command.StartsWith("/action/restartfile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.ForceStop = true;
                    Program.queued_files.Remove(workid);
                    Program.dump_files(f.PostFile);
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/stopandbanfile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.ForceStop = true;
                    Program.ban_file(f.Hash);
                    f.Log(new LogEntry() { Level = LogEntry.LogLevel.Success, Message = "File was banned", Sender = "-", Title = "" });
                    // Program.queued_files.Remove(workid);
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/removefile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    Program.queued_files.Remove(workid);
                    response.Redirect("/fq");
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/enablefullfile"))
            {
                Program.thumb_only = false;
                response.Redirect("/");
                return true;
            }

            if (command.StartsWith("/action/resetfileretrycount/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.RetryCount = 0;
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/forcestopfile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.ForceStop = true;
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            return false;
        }

        public static void _404(HttpServer.IHttpResponse response)
        {
            response.Status = System.Net.HttpStatusCode.NotFound;
            byte[] d = System.Text.Encoding.UTF8.GetBytes("404");
            response.ContentLength = d.Length;
            response.SendHeaders();
            response.SendBody(d);
        }

        public static void write_text(string text, HttpServer.IHttpResponse response)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            response.ContentLength = data.Length;
            response.SendHeaders();
            response.SendBody(data);
        }

        public static string get_board_list(string name)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("<select name=\"{0}\" class=\"form-control\">", name);

            foreach (KeyValuePair<string, string> bb in Program.ValidBoards)
            {
                sb.AppendFormat("<option value='{0}'>{0} - {1}</option>", bb.Key, bb.Value);
            }

            sb.Append("</select>");
            return sb.ToString();
        }


        private PostFormatter load_post_data(FileInfo fi, bool isop)
        {
            return load_post_data_str(File.ReadAllText(fi.FullName), isop);
        }

        private PostFormatter load_post_data_str(string data, bool isop)
        {
            Dictionary<string, object> post_data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

            PostFormatter pf = new PostFormatter();

            if (post_data.ContainsKey("RawComment"))
            {
                pf.Comment = Convert.ToString(post_data["RawComment"]);
            }

            if (post_data.ContainsKey("Email"))
            {
                pf.Email = Convert.ToString(post_data["Email"]);
            }
            if (post_data.ContainsKey("Name"))
            {
                pf.Name = Convert.ToString(post_data["Name"]);
            }

            if (post_data.ContainsKey("PosterID"))
            {
                pf.PosterID = Convert.ToString(post_data["PosterID"]);
            }

            if (post_data.ContainsKey("Subject"))
            {
                pf.Subject = Convert.ToString(post_data["Subject"]);
            }
            if (post_data.ContainsKey("Trip"))
            {
                pf.Trip = Convert.ToString(post_data["Trip"]);
            }

            if (post_data.ContainsKey("ID"))
            {
                pf.PostID = Convert.ToInt32(post_data["ID"]);
            }

            if (post_data.ContainsKey("Time"))
            {
                pf.Time = Convert.ToDateTime(post_data["Time"]);
            }

            if (post_data.ContainsKey("FileHash"))
            {
                FileFormatter f = new FileFormatter();

                f.PostID = pf.PostID;

                if (post_data.ContainsKey("FileName"))
                {
                    f.FileName = Convert.ToString(post_data["FileName"]);
                }

                if (post_data.ContainsKey("FileHash"))
                {
                    f.Hash = Convert.ToString(post_data["FileHash"]);
                }
                if (post_data.ContainsKey("ThumbTime"))
                {
                    f.ThumbName = Convert.ToString(post_data["ThumbTime"]);
                }

                if (post_data.ContainsKey("FileHeight"))
                {
                    f.Height = Convert.ToInt32(post_data["FileHeight"]);
                }

                if (post_data.ContainsKey("FileWidth"))
                {
                    f.Width = Convert.ToInt32(post_data["FileWidth"]);
                }

                if (post_data.ContainsKey("FileSize"))
                {
                    f.Size = Convert.ToInt32(post_data["FileSize"]);
                }

                pf.MyFile = f;
            }

            if (isop)
            {
                if (post_data.ContainsKey("Closed"))
                {
                    pf.IsLocked = Convert.ToBoolean(post_data["Closed"]);
                }

                if (post_data.ContainsKey("Sticky"))
                {
                    pf.IsSticky = Convert.ToBoolean(post_data["Sticky"]);
                }

                pf.Type = PostFormatter.PostType.OP;
            }
            else
            {
                pf.Type = PostFormatter.PostType.Reply;
            }

            return pf;
        }

    }
}
