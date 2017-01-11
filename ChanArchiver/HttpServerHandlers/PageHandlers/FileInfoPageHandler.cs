using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public sealed class FileInfoPageHandler
        : PageHandlerBase
    {
        public const string Url = "/fileinfo";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command.StartsWith(Url))
            {
                string file_hash = request.QueryString[UrlParameters.FileHash].Value;

                FileQueueStateInfo fqs = Program.get_file_state(file_hash);

                if (fqs != null)
                {
                    StringBuilder page = new StringBuilder(HtmlTemplates.FileInfoPageTemplate);

                    IncludeCommonHtml(page);

                    page.Replace("{fullfilelink}", string.Format("/file/{0}.{1}", fqs.Hash, fqs.Ext));

                    page.Replace("{thumbsource}", string.Format("/thumb/{0}.jpg", fqs.Hash));

                    page.Replace("{url}", fqs.Url);

                    page.Replace("{p}", fqs.Percent().ToString());
                    page.Replace("{md5}", fqs.Hash);

                    page.Replace("{name}", fqs.FileName);

                    page.Replace("{workid}", file_hash);

                    page.Replace("{jtype}", fqs.Type.ToString());
                    page.Replace("{rcount}", fqs.RetryCount.ToString());

                    page.Replace("{downloaded}", Program.format_size_string(fqs.Downloaded));
                    page.Replace("{length}", Program.format_size_string(fqs.Length));

                    page.Replace("{Logs}", get_logs(fqs.Logs));

                    WriteFinalHtmlResponse(response, page.ToString());

                    return true;
                }
                else
                {
                    response.Redirect(FileQueuePageHandler.Url);
                    return true;
                }
            }

            return false;
        }

        public static string GetLinkToThisPage(string fileHash)
        {
            return string.Format("{0}?{1}={2}", Url, UrlParameters.FileHash, fileHash);
        }

        public override PageType GetPageType()
        {
            return PageType.WatchJobs;
        }

        public override string GetPageTitle()
        {
            return "File Info";
        }

        private static string get_logs(LogEntry[] logs)
        {
            StringBuilder items = new StringBuilder();

            for (int index = 0; index < logs.Length; index++)
            {

                try
                {
                    LogEntry e = logs[index];

                    items.Append("<tr>");

                    switch (e.Level)
                    {
                        case LogEntry.LogLevel.Fail:
                            items.Append("<td><span class=\"label label-danger\">Fail</span></td>");
                            break;
                        case LogEntry.LogLevel.Info:
                            items.Append("<td><span class=\"label label-info\">Info</span></td>");
                            break;
                        case LogEntry.LogLevel.Success:
                            items.Append("<td><span class=\"label label-success\">Success</span></td>");
                            break;
                        case LogEntry.LogLevel.Warning:
                            items.Append("<td><span class=\"label label-warning\">Warning</span></td>");
                            break;
                        default:
                            items.Append("<td><span class=\"label label-default\">Unknown</span></td>");
                            break;
                    }

                    items.AppendFormat("<td>{0}</td>", e.Time);
                    items.AppendFormat("<td>{0}</td>", e.Title);
                    items.AppendFormat("<td>{0}</td>", e.Sender);
                    items.AppendFormat("<td>{0}</td>", e.Message);

                    items.Append("</tr>");

                }
                catch (Exception) { continue; }

            }
            return items.ToString();
        }
    }
}
