using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class FileQueuePageHandler : HttpServer.HttpModules.HttpModule
    {

        public static void IncrementKey(Dictionary<string, int> a, string key)
        {
            if (a.ContainsKey(key)) { a[key]++; } else { a.Add(key, 1); }
        }

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command == "/fq" || command == "/fq/")
            {
                StringBuilder sb = new StringBuilder();

                var ordered = Program.queued_files.OrderByDescending(x => x.Value.Percent());

                int count = ordered.Count();

                double total_size = 0;

                Dictionary<string, int> exts_stats = new Dictionary<string, int>();
                Dictionary<string, int> board_stats = new Dictionary<string, int>();
                // {qstats}

                for (int index = 0; index < count; index++)
                {
                    try
                    {
                        KeyValuePair<string, FileQueueStateInfo> kvp = ordered.ElementAt(index);

                        FileQueueStateInfo f = kvp.Value;

                        if ((!Settings.ListThumbsInQueue) && f.Type == FileQueueStateInfo.FileType.Thumbnail)
                        {
                            continue;
                        }

                        //sb.AppendFormat("<tr id='{0}'>", kvp.Key);

                        sb.Append("<tr>");

                        sb.AppendFormat("<td>{0}</td>", get_file_status(f.Status));

                        sb.AppendFormat("<td>{0}</td>", f.RetryCount);

                        sb.AppendFormat("<td>{0}</td>", get_type(f.Type));
                        sb.AppendFormat("<td>{0}</td>", f.Hash);

                        if (f.Type == FileQueueStateInfo.FileType.FullFile)
                        {
                            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(f.PostFile.size));

                            if (f.Status != FileQueueStateInfo.DownloadStatus.Complete || f.Status != FileQueueStateInfo.DownloadStatus.NotFound)
                            {
                                total_size += f.PostFile.size;
                            }
                        }
                        else
                        {
                            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(f.Length));

                            //complete files are not in the queue any more
                            if (f.Status != FileQueueStateInfo.DownloadStatus.Complete || f.Status != FileQueueStateInfo.DownloadStatus.NotFound)
                            {
                                total_size += f.Length;
                            }

                        }

                        sb.AppendFormat("<td>/{0}/</td>", f.PostFile.board);

                        sb.AppendFormat("<td><code><a href='/boards/{0}/{1}'>{1}</a></code></td>", f.PostFile.board, f.PostFile.owner.OwnerThread.ID);

                        sb.AppendFormat("<td><code><a href='/boards/{0}/{1}#p{2}'>{2}</a></code></td>", f.PostFile.board, f.PostFile.owner.OwnerThread.ID, f.PostFile.owner.ID);

                        if (f.Type == FileQueueStateInfo.FileType.Thumbnail)
                        {
                            sb.Append("<td><span class=\"label label-info\">JPG</span></td>");
                            IncrementKey(exts_stats, "jpg");
                        }
                        else
                        {
                            sb.AppendFormat("<td>{0}</td>", get_ext(f.PostFile.ext));
                            IncrementKey(exts_stats, f.PostFile.ext);
                        }

                        sb.AppendFormat("<td>{0} %</td>", Math.Round(f.Percent(), 2));

                        sb.AppendFormat("<td> <a href=\"/fileinfo/{0}\" class=\"label label-primary\">Info</a> </td>", kvp.Key);

                        sb.AppendFormat("<td><span class=\"label label-primary\">{0}</span></td>", f.Type == FileQueueStateInfo.FileType.FullFile ? f.Priority.ToString().Replace("Level", "") : "N/A");

                        sb.Append("</tr>");


                        IncrementKey(board_stats, f.PostFile.board);


                    }
                    catch (Exception)
                    {
                        if (index >= Program.queued_files.Keys.Count) { break; }
                    }
                }


                StringBuilder stats = new StringBuilder();

                stats.AppendFormat("<p>{0}: <b>{1}</b></p>", "Total file in the queue", count);

                stats.AppendFormat("<p>{0}: <b>{1}</b></p>", "Total files size", Program.format_size_string(total_size));

                if (board_stats.Count > 0)
                {

                    stats.Append("<p><ul>");

                    foreach (KeyValuePair<string, int> v in board_stats)
                    {
                        stats.AppendFormat("<li>{0} file for /{1}/</li>", v.Value, v.Key);
                    }

                    stats.Append("</ul></p>");
                }

                if (exts_stats.Count > 0)
                {

                    stats.Append("<p><ul>");

                    foreach (KeyValuePair<string, int> v in exts_stats)
                    {
                        stats.AppendFormat("<li>{0} file is {1}</li>", v.Value, get_ext(v.Key));
                    }

                    stats.Append("</ul></p>");
                }

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.filequeue_page.Replace("{qstats}", stats.ToString()).Replace("{mff}", Program.file_stp.MaxThreads.ToString()).Replace("{Files}", sb.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            if (command == "/fq/list.txt") 
            {
                StringBuilder sb = new StringBuilder();

                var ordered = Program.queued_files;

                int count = ordered.Count();


                for (int index = 0; index < count; index++)
                {
                    try
                    {
                        KeyValuePair<string, FileQueueStateInfo> kvp = ordered.ElementAt(index);

                        FileQueueStateInfo f = kvp.Value;

                        if (f.Type == FileQueueStateInfo.FileType.Thumbnail)
                        {
                            continue;
                        }
                   
                        sb.AppendFormat("<a href='{0}'>{0}</a><br/>", f.Url);

                    }
                    catch (Exception)
                    {
                        if (index >= Program.queued_files.Keys.Count) { break; }
                    }
                }

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            if (command.StartsWith("/fq/json/"))
            {
                if (string.IsNullOrEmpty(request.QueryString["hash"].Value))
                {
                    ThreadServerModule._404(response);
                }
                else
                {
                    bool ifo = !string.IsNullOrWhiteSpace(request.QueryString["ifo"].Value);

                    if (Program.queued_files.ContainsKey(request.QueryString["hash"].Value))
                    {
                        FileQueueStateInfo f = Program.queued_files[request.QueryString["hash"].Value];

                        Dictionary<string, object> dt = new Dictionary<string, object>(3);

                        if (ifo)
                        {
                            dt.Add("p", f.Percent().ToString());
                            dt.Add("s", string.Format("{0} / {1}", Program.format_size_string(f.Downloaded), Program.format_size_string(f.Length)));
                            dt.Add("c", f.Status == FileQueueStateInfo.DownloadStatus.Complete);
                        }
                        else
                        {
                            dt.Add("Status", get_file_status(f.Status));
                            dt.Add("RetryCount", f.RetryCount);
                            dt.Add("Percent", string.Format("{0} %", Math.Round(f.Percent(), 2)));
                        }
                        response.Status = System.Net.HttpStatusCode.OK;
                        response.ContentType = "application/json";

                        ThreadServerModule.write_text(Newtonsoft.Json.JsonConvert.SerializeObject(dt), response);
                    }
                }
                return true;
            }


            return false;
        }

        private string get_ext(string ext)
        {
            switch (ext)
            {
                case "jpg":
                    return "<span class=\"label label-info\">JPG</span>";
                case "png":
                    return ("<span class=\"label label-warning\">PNG</span>");
                case "gif":
                    return ("<span class=\"label label-danger\">GIF</span>");
                case "pdf":
                    return ("<span class=\"label label-success\">PDF</span>");
                case "webm":
                    return ("<span class=\"label label-default\">WebM</span>");
                case "swf":
                default:
                    return string.Format("<span class=\"label label-default\">{0}</span>", ext.ToUpper());
            }
        }

        private string get_type(FileQueueStateInfo.FileType ty)
        {
            if (ty == FileQueueStateInfo.FileType.FullFile)
            {
                return "<span class=\"label label-warning\">Full File</span>";
            }
            else
            {
                return "<span class=\"label label-danger\">Thumb</span>";
            }
        }

        private string get_file_status(FileQueueStateInfo.DownloadStatus s)
        {
            switch (s)
            {
                case FileQueueStateInfo.DownloadStatus.Downloading:
                    return "<span class=\"label label-info\">Downloading</span>";
                case FileQueueStateInfo.DownloadStatus.Connecting:
                    return ("<span class=\"label label-warning\">Connecting</span>");
                case FileQueueStateInfo.DownloadStatus.Error:
                    return ("<span class=\"label label-danger\">Error</span>");
                case FileQueueStateInfo.DownloadStatus.Complete:
                    return ("<span class=\"label label-success\">Complete</span>");
                case FileQueueStateInfo.DownloadStatus.Queued:
                    return ("<span class=\"label label-default\">Queued</span>");
                case FileQueueStateInfo.DownloadStatus.NotFound:
                    return ("<span class=\"label label-danger\">404</span>");
                case FileQueueStateInfo.DownloadStatus.Stopped:
                    return ("<span class=\"label label-primary\">Stopped</span>");

                default:
                    return string.Format("<span class=\"label label-default\">{0}</span>", s);
            }
        }

    }


}
