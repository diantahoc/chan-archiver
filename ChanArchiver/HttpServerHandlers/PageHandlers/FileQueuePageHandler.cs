using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public class FileQueuePageHandler
        : PageHandlerBase
    {
        public const string Url = "/fq";

        // Used for file queue statistics
        private Dictionary<string, int> exts_stats = new Dictionary<string, int>();
        private Dictionary<string, int> board_stats = new Dictionary<string, int>();
        private int files_count = 0;
        private double total_files_size = 0;

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath == Url || request.UriPath == (Url + "/"))
            {
                StringBuilder sb = new StringBuilder(HtmlTemplates.FileQueuePageTemplate);

                IncludeCommonHtml(sb);

                sb.Replace("{{files-table}}", GetFileQueueTableHtml());

                sb.Replace("{{file-queue-statistics}}", GetFileStatsHtml());

                sb.Replace("{{settings-max-parallel-files-download}}", Program.file_stp.MaxThreads.ToString());

                WriteFinalHtmlResponse(response, sb.ToString());

                return true;
            }

            /*if (command == "/fq/list.txt")
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
            }*/
            return false;
        }

        private string GetFileQueueTableHtml()
        {
            StringBuilder sb = new StringBuilder();

            var ordered = Program.queued_files.OrderByDescending(x => x.Value.Percent());

            files_count = ordered.Count();

            for (int index = 0; index < files_count; index++)
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
                            total_files_size += f.PostFile.size;
                        }
                    }
                    else
                    {
                        sb.AppendFormat("<td>{0}</td>", Program.format_size_string(f.Length));

                        //complete files are not in the queue any more
                        if (f.Status != FileQueueStateInfo.DownloadStatus.Complete || f.Status != FileQueueStateInfo.DownloadStatus.NotFound)
                        {
                            total_files_size += f.Length;
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
            return sb.ToString();
        }

        private string GetFileStatsHtml()
        {
            StringBuilder stats = new StringBuilder();

            stats.AppendFormat("<p>{0}: <b>{1}</b></p>", "Total file in the queue", files_count);

            stats.AppendFormat("<p>{0}: <b>{1}</b></p>", "Total files size", Program.format_size_string(total_files_size));

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

            return stats.ToString();
        }

        private void IncrementKey(Dictionary<string, int> a, string key)
        {
            if (a.ContainsKey(key)) { a[key]++; } else { a.Add(key, 1); }
        }

        public override PageType GetPageType()
        {
            return PageType.FileQueue;
        }

        public override string GetPageTitle()
        {
            return "File Queue";
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
