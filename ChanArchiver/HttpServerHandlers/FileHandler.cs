using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver.HttpServerHandlers
{
    public class FileHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command.StartsWith("/file/"))
            {
                string hash = command.Split('/').Last();
                string path = Path.Combine(Program.file_save_dir, hash);
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
                    byte[] data = Properties.Resources._4;

                    if (Program.is_file_banned(hash))
                    {
                        data = Properties.Resources._b;
                    }

                    response.ContentType = "image/gif";
                    response.Status = System.Net.HttpStatusCode.NotFound;
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);
                }
                return true;
            }

            if (command.StartsWith("/filecn/"))
            {
                string hash = command.Split('?').First().Split('/').Last();
                string path = Path.Combine(Program.file_save_dir, hash);
                if (File.Exists(path))
                {
                    string custom_name = request.QueryString["cn"].Value;

                    if (!string.IsNullOrEmpty(custom_name))
                    {
                        response.AddHeader("content-disposition", string.Format("attachment; filename=\"{0}\"", custom_name));
                    }

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

            return false;
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
                case "webm":
                    return "video/webm";
                case "pdf":
                    return "application/pdf";
                case "swf":
                    return "application/x-shockwave-flash";
                default:
                    return "application/octect-stream";
            }
        }

    }
}
