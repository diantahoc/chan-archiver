using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
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
                string extension = command.Split('.').Last().ToLower();

                string ua = request.Headers["User-Agent"].ToLower();
                bool no_webm = device_not_support_webm(ua);

                FileInfo fi = new FileInfo(path);
                

                if (fi.Exists && fi.DirectoryName == Program.file_save_dir)
                {

                    #region WebM to MP4

                    if (extension == "webm" && Settings.ConvertWebmToMp4)
                    {
                        if (File.Exists(Program.ffmpeg_path) && no_webm)
                        {
                            //convert the video for the user
                            ProcessStartInfo psr = new System.Diagnostics.ProcessStartInfo(Program.ffmpeg_path);

                            string temp_path = Path.Combine(Program.temp_files_dir, "con-" + hash + ".mp4");

                            File.Delete(temp_path);

                            psr.CreateNoWindow = true;
                            psr.UseShellExecute = false;

                            psr.Arguments = string.Format("-y -i \"{0}\" -c:v libx264 -preset ultrafast -vf scale=320:240 -threads 2 \"{1}\"", path, temp_path);

                            psr.RedirectStandardOutput = true;

                            using (Process proc = System.Diagnostics.Process.Start(psr))
                            {
                                proc.WaitForExit(20000);
                                if (!proc.HasExited) { proc.Kill(); }
                            }

                            if (File.Exists(temp_path))
                            {
                                byte[] converted_data = File.ReadAllBytes(temp_path);
                                response.Status = System.Net.HttpStatusCode.OK;
                                response.ContentType = "video/mpeg";
                                response.ContentLength = converted_data.Length;
                                response.AddHeader("content-disposition", string.Format("inline; filename=\"{0}\"", hash + ".mp4"));
                                response.SendHeaders();
                                response.SendBody(converted_data);
                                File.Delete(temp_path);
                                return true;
                            }
                        }
                    } // webm to mp4 check

                    #endregion

                    response.ContentType = get_mime(extension);
                    response.Status = System.Net.HttpStatusCode.OK;

                    byte[] data = File.ReadAllBytes(path);
                    response.ContentLength = data.Length;

                    response.SendHeaders();

                    response.SendBody(data);
                    return true;
                }
                // probably this gif file has been converted to a webm
                else if (fi.DirectoryName == Program.file_save_dir && File.Exists(path + ".webm"))
                {
                    string was_gif_path = path + ".webm";

                    if (Settings.Convert_Webmgif_To_Target /*the general switch to gif to x*/ &&
                        (!Settings.Convert_Webmgif_only_devices || (Settings.Convert_Webmgif_only_devices && no_webm)))
                    {
                        if (File.Exists(Program.ffmpeg_path))
                        {
                            ProcessStartInfo psr = new System.Diagnostics.ProcessStartInfo(Program.ffmpeg_path)
                            {
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                            };

                            string temp_path = "";

                            if (Settings.Convert_Webmgif_Target == Settings.X_Target.GIF)
                            {
                                temp_path = Path.Combine(Program.temp_files_dir, "con-" + hash + ".gif");
                                psr.Arguments = string.Format("-y -i \"{0}\" -threads 2 \"{1}\"", was_gif_path, temp_path);
                                response.ContentType = "image/gif";

                            }
                            else
                            {
                                temp_path = Path.Combine(Program.temp_files_dir, "con-" + hash + ".mp4");
                                psr.Arguments = string.Format("-y -i \"{0}\" -threads 2 -c:v libx264 -preset ultrafast \"{1}\"", was_gif_path, temp_path);
                                response.ContentType = "video/mpeg";
                            }

                            File.Delete(temp_path);

                            using (Process proc = System.Diagnostics.Process.Start(psr))
                            {
                                proc.WaitForExit(20000);
                                if (!proc.HasExited) { proc.Kill(); }
                            }

                            if (File.Exists(temp_path))
                            {
                                byte[] converted_data = File.ReadAllBytes(temp_path);
                                response.Status = System.Net.HttpStatusCode.OK;
                                response.ContentLength = converted_data.Length;

                                if (Settings.Convert_Webmgif_Target == Settings.X_Target.GIF)
                                {
                                    response.AddHeader("content-disposition", string.Format("inline; filename=\"{0}\"", hash + ".gif"));
                                }
                                else
                                {
                                    response.AddHeader("content-disposition", string.Format("inline; filename=\"{0}\"", hash + ".mp4"));
                                }

                                response.SendHeaders();
                                response.SendBody(converted_data);
                                File.Delete(temp_path);
                                return true;
                            }
                            else
                            {
                                goto aw;
                            }
                        }
                    } // (wg --> x)

                aw:

                    //other wise send it as webm
                    response.ContentType = "video/webm";
                    response.Status = System.Net.HttpStatusCode.OK;
                    byte[] data = File.ReadAllBytes(was_gif_path);
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);

                    return true;

                }
                else
                {
                    byte[] data = Properties.Resources._4;

                    //if (Program.is_file_banned(hash))
                    //{
                    //    data = Properties.Resources._b;
                    //}

                    response.ContentType = "image/gif";
                    response.Status = System.Net.HttpStatusCode.NotFound;
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);
                    return true;
                }
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
                else if (File.Exists(path + ".webm"))
                {
                    response.ContentType = "video/webm";
                    response.Status = System.Net.HttpStatusCode.OK;
                    byte[] data = File.ReadAllBytes(path + ".webm");
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);

                    return true;
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

                FileInfo fi = new FileInfo(path);

                if (fi.Exists && fi.DirectoryName == Program.thumb_save_dir)
                {
                    response.ContentType = "image/jpeg";
                    response.Status = System.Net.HttpStatusCode.OK;

                    response.AddHeader("cache-control", "max-age=3600");

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

        private bool device_not_support_webm(string ua)
        {
            return ua.Contains("s60") || ua.Contains("symbos");
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
