using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class FileInfoPageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command.StartsWith("/fileinfo/"))
            {

                string[] a = command.Split('/');

                if (a.Length >= 3)
                {
                    string filehash = a[2];

                    FileQueueStateInfo fqs = Program.get_file_state(filehash);

                    if (fqs != null)
                    {

                        StringBuilder page = new StringBuilder(Properties.Resources.fileinfo);

                        page.Replace("{fullfilelink}", string.Format("/file/{0}.{1}", fqs.Hash, fqs.Ext));

                        page.Replace("{thumbsource}", string.Format("/thumb/{0}.jpg", fqs.Hash));

                        page.Replace("{url}", fqs.Url);

                        page.Replace("{p}", fqs.Percent().ToString());
                        page.Replace("{md5}", fqs.Hash);

                        page.Replace("{name}", fqs.FileName);

                        page.Replace("{workid}", filehash);

                        page.Replace("{jtype}", fqs.Type.ToString());
                        page.Replace("{rcount}", fqs.RetryCount.ToString());

                        page.Replace("{downloaded}", Program.format_size_string(fqs.Downloaded));
                        page.Replace("{length}", Program.format_size_string(fqs.Length));

                        page.Replace("{Logs}", get_logs(fqs.Logs));


                        response.Status = System.Net.HttpStatusCode.OK;
                        response.ContentType = "text/html";

                        byte[] data = Encoding.UTF8.GetBytes(page.ToString());
                        response.ContentLength = data.Length;
                        response.SendHeaders();
                        response.SendBody(data);

                        return true;

                    }
                    else
                    {
                        return false;
                    }


                }
            }



            return false;
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
