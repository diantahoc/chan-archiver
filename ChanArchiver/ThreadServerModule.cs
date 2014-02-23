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
            response.Encoding = System.Text.Encoding.UTF8;

            string command = request.UriPath.ToString();

            if (command.StartsWith("/file/"))
            {
                string path = Path.Combine(Program.file_save_dir, command.Split('/').Last());
                if (File.Exists(path))
                {
                    response.ContentType = get_mime(command.Split('.').Last().ToLower());
                    response.Status = System.Net.HttpStatusCode.OK;

                    byte[] data = File.ReadAllBytes(path);
                    response.ContentLength = data.Length;

                    response.SendHeaders();

                    response.SendBody(data);
                }
                else
                {
                    response.ContentType = "image/gif";
                    response.Status = System.Net.HttpStatusCode.NotFound;
                    response.ContentLength = Properties.Resources._4.Length;
                    response.SendHeaders();
                    response.SendBody(Properties.Resources._4);
                }
                return true;
            }

            if (command.StartsWith("/thumb/"))
            {
                string path = Path.Combine(Program.thumb_save_dir, command.Split('/').Last());

                if (File.Exists(path))
                {
                    response.ContentType = "image/jpeg";
                    response.Status = System.Net.HttpStatusCode.OK;

                    byte[] data = File.ReadAllBytes(path);
                    response.ContentLength = data.Length;

                    response.SendHeaders();

                    response.SendBody(data);
                }
                else
                {
                    response.ContentType = "image/gif";
                    response.Status = System.Net.HttpStatusCode.NotFound;
                    response.ContentLength = Properties.Resources._4.Length;
                    response.SendHeaders();
                    response.SendBody(Properties.Resources._4);
                }
                return true;
            }

            if (command == "/css/css.css")
            {
                response.ContentType = "text/css";
                response.Status = System.Net.HttpStatusCode.OK;
                byte[] css = System.Text.Encoding.UTF8.GetBytes(ChanArchiver.Properties.Resources.layout);
                response.ContentLength = css.Length;
                response.SendHeaders();
                response.SendBody(css);

                return true;
            }

            if (command.StartsWith("/view/"))
            {
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

                string thread_folder_path = Path.Combine(Program.program_dir, board, tid);

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

                    for (int i = 0; i < cou-1; i++)
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

            if (command == "/")
            {
                DirectoryInfo i = new DirectoryInfo(Program.program_dir);

                DirectoryInfo[] boards = i.GetDirectories();

                response.ContentType = "text/html";
                response.Status = System.Net.HttpStatusCode.OK;
               

                StringBuilder s = new StringBuilder();

                for (int index = 0; index < boards.Length; index++)
                {
                    DirectoryInfo fo = boards[index];
                    if (fo.Name == "files" || fo.Name == "thumbs" || fo.Name == "aniwrap_cache") { continue; }
                    s.AppendFormat("<a href='/all/{0}/'>/{0}/</a><br/>", fo.Name);
                }

                byte[] data = System.Text.Encoding.UTF8.GetBytes(Properties.Resources.full_page.Replace("{DocumentBody}", s.ToString()));

                response.ContentLength = data.Length;
                
                response.SendHeaders();

                response.SendBody(data);

                return true;
            }

            if (command.StartsWith("/all/"))
            {
                string board = command.Split('/')[2];

                string board_folder = Path.Combine(Program.program_dir, board);

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

            if (command.StartsWith("/res/"))
            {
                byte[] data = null;
                switch (command.Split('/')[2].ToLower())
                {
                    case "bgwhite.png":
                        data = Properties.Resources.bgwhite;
                        break;
                    case "hr.png":
                        data = Properties.Resources.hr;
                        break;
                    case "locked.png":
                        data = Properties.Resources.locked;
                        break;
                    case "sticky":
                        data = Properties.Resources.sticky;
                        break;
                    default:
                        break;
                }

                if (data == null)
                {
                    _404(response);
                }
                else
                {
                    response.Status = System.Net.HttpStatusCode.OK;

                    response.ContentType = "image/png";
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);
                }

            }



            return false;
        }

        private void _404(HttpServer.IHttpResponse response)
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

        private string get_mime(string ext)
        {
            switch (ext)
            {
                case "jpg":
                case "jpeg":
                    return "image/jpeg";
                case "png":
                    return "image/png";
                case "gif":
                    return "image/gif";
                default:
                    return "application/octect-stream";
            }
        }
    }
}
