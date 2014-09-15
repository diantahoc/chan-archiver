using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class ThreadJobInfoPageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command.StartsWith("/threadinfo"))
            {
                string board = request.QueryString["board"].Value;
                string id = request.QueryString["id"].Value;

                if (string.IsNullOrEmpty(board) || string.IsNullOrEmpty(id))
                {
                    response.Redirect("/wjobs"); return true;
                }

                if (Program.active_dumpers.ContainsKey(board))
                {
                    BoardWatcher bw = Program.active_dumpers[board];

                    int tid = 0;

                    Int32.TryParse(id, out tid);

                    if (bw.watched_threads.ContainsKey(tid))
                    {
                        ThreadWorker tw = bw.watched_threads[tid];

                        StringBuilder properties = new StringBuilder();

                        properties.AppendFormat("<span>{0}: </span><code>{1}</code><br/>", "Thread ID", tw.ID);

                        properties.AppendFormat("<span>{0}: </span><code>{1}</code><br/>", "Board", tw.Board.Board);

                        properties.AppendFormat("<span>{0}: </span><code>{1}</code><br/>", "Update Interval (in min)", tw.UpdateInterval);

                        properties.AppendFormat("<span>{0}: </span><code>{1}</code><br/>", "BumpLimit", tw.BumpLimit);

                        properties.AppendFormat("<span>{0}: </span><code>{1}</code><br/>", "ImageLimit", tw.ImageLimit);

                        response.Status = System.Net.HttpStatusCode.OK;
                        response.ContentType = "text/html";

                        byte[] data = Encoding.UTF8.GetBytes
                            (Properties.Resources.threadinfo_page
                            .Replace("{properties}", properties.ToString())
                            .Replace("{Logs}", get_logs(tw.Logs)));

                        response.ContentLength = data.Length;
                        response.SendHeaders();
                        response.SendBody(data);

                        return true;
                    }
                }

                response.Redirect("/wjobs"); return true;
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
