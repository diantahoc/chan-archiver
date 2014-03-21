using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver
{
    public class ThreadServerModule : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command.StartsWith("/view/"))
            {
                response.Encoding = System.Text.Encoding.UTF8;
                response.ContentType = "text/html";
                response.Status = System.Net.HttpStatusCode.OK;

                string[] data = command.Split('/');
                if (data.Length != 4)
                {
                    _404(response);
                    return true;
                }
                string board = data[2];
                string tid = data[3];

                string thread_folder_path = Path.Combine(Program.post_files_dir, board, tid);

                if (Directory.Exists(thread_folder_path))
                {

                    StringBuilder body = new StringBuilder();

                    body.AppendFormat("<div class=\"thread\" id=\"t{0}\">", tid);

                    DirectoryInfo info = new DirectoryInfo(thread_folder_path);

                    FileInfo[] files = info.GetFiles("*.json", SearchOption.TopDirectoryOnly);

                    body.Append(load_post_data(new FileInfo(Path.Combine(thread_folder_path, "op.json")), true));

                    body.Replace("{op:replycount}", Convert.ToString(files.Count() - 1));

                    IOrderedEnumerable<FileInfo> sorted = files.OrderBy(x => x.Name);

                    int cou = sorted.Count();

                    for (int i = 0; i < cou - 1; i++)
                    {
                        body.Append(load_post_data(sorted.ElementAt(i), false));
                    }

                    body.Append("</div>");


                    byte[] respon = System.Text.Encoding.UTF8.GetBytes(Properties.Resources.full_page.Replace("{DocumentBody}", body.ToString()));

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

            if (command.StartsWith("/all/"))
            {
                response.Encoding = System.Text.Encoding.UTF8;
                string board = command.Split('/')[2];

                if (string.IsNullOrEmpty(board))
                {
                    _404(response);
                    return true;
                }

                string board_folder = Path.Combine(Program.post_files_dir, board);

                if (Directory.Exists(board_folder))
                {

                    response.ContentType = "text/html";
                    response.Status = System.Net.HttpStatusCode.OK;

                    DirectoryInfo info = new DirectoryInfo(board_folder);

                    DirectoryInfo[] folders = info.GetDirectories();

                    StringBuilder s = new StringBuilder();

                    for (int i = 0; i < folders.Length; i++)
                    {

                        string op_file = Path.Combine(folders[i].FullName, "op.json");
                        if (File.Exists(op_file))
                        {

                            FileInfo ifo = new FileInfo(op_file);

                            s.Append("<thread>");

                            // s.AppendFormat("<a href='/view/{0}/{1}'>{1}</a><br/>", board, folders[i].Name);

                            s.Append
                                (
                                      load_post_data(ifo, true)
                                      .Replace("{op:replycount}", "")
                                      .Replace("{postLink}", string.Format("/view/{0}/{1}", board, folders[i].Name))
                                );

                            s.Append("</thread><hr/>");
                        }
                    }

                    byte[] data = System.Text.Encoding.UTF8.GetBytes(Properties.Resources.full_page.Replace("{DocumentBody}", s.ToString()));

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

            if (command == "/boards")
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
                        s.AppendFormat("<a href='/all/{0}'>/{0}/</a><br/>", folders[i].Name);
                    }

                    byte[] data = System.Text.Encoding.UTF8.GetBytes(Properties.Resources.full_page.Replace("{DocumentBody}", s.ToString()));

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
                        response.Redirect("/wjobs");
                    }

                }

                if (mode == "bwr")
                {
                    string board = data[3];
                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        bw.StartMonitoring(BoardWatcher.BoardMode.FullBoard);
                        response.Redirect("/wjobs");
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

            if (command.StartsWith("/action/resetcount/"))
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

        private string load_post_data(FileInfo fi, bool isop)
        {
            Dictionary<string, object> post_data = (Dictionary<string, object>)Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(fi.FullName), typeof(Dictionary<string, object>));

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

            return pf.ToString();
        }

    }
}
