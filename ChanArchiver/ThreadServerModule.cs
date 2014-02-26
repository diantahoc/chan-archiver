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
                        s.AppendFormat("<a href='/view/{0}/{1}'>{1}</a><br/>", board, folders[i].Name);
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
                    Program.archive_board(board);
                    response.Status = System.Net.HttpStatusCode.OK;

                    response.Redirect("/wjobs");

                }
                else if (mode == "thread")
                {
                    if (string.IsNullOrEmpty(request.QueryString["boardletter"].Value))
                    {
                        _404(response);
                    }

                    if (string.IsNullOrEmpty(request.QueryString["threadid"].Value))
                    {
                        _404(response);
                    }

                    string board = request.QueryString["boardletter"].Value;
                    int id = -1;
                    string tid = request.QueryString["threadid"].Value;
                    Int32.TryParse(tid, out id);
                    if (id > 0)
                    {
                        Program.archive_single(board, Convert.ToInt32(tid));
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
                        bw.Stop();
                        response.Redirect("/wjobs");
                    }

                }

                if (mode == "bwr")
                {
                    string board = data[3];
                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        bw.StartFullMode();
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
