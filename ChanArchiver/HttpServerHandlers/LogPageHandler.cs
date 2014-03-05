using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class LogPageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command.StartsWith("/logs/"))
            {
                string[] a = command.Split('/');

                if (a.Length >= 3)
                {

                    string mode = a[2];

                    string id = a[3];

                    LogEntry[] data = null;

                    switch (mode)
                    {
                        case "file":
                            FileQueueStateInfo st = Program.get_file_state(id);
                            if (st != null)
                            {
                                data = st.Logs;
                            }
                            break;
                        case "threadworker": // /logs/threadworker/board/tid
                            if (Program.active_dumpers.ContainsKey(id))
                            {
                                BoardWatcher bw = Program.active_dumpers[id];
                                if (bw.watched_threads.ContainsKey(Convert.ToInt32(a[4])))
                                {
                                    data = bw.watched_threads[Convert.ToInt32(a[4])].Logs;
                                }
                            }
                            break;
                        case "boardwatcher":
                            if (Program.active_dumpers.ContainsKey(id))
                            {
                                BoardWatcher bw = Program.active_dumpers[id];
                                data = bw.Logs;
                            }
                            break;
                        default:
                            break;
                    }

                    if (data != null)
                    {
                        StringBuilder items = new StringBuilder();

                        for (int index = 0; index < data.Length; index++)
                        {

                            try
                            {
                                LogEntry e = data[index];

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

                        //write everything
                        response.Status = System.Net.HttpStatusCode.OK;
                        response.ContentType = "text/html";

                        byte[] fdata = Encoding.UTF8.GetBytes(Properties.Resources.logs_page.Replace("{Logs}", items.ToString()));
                        response.ContentLength = fdata.Length;
                        response.SendHeaders();
                        response.SendBody(fdata);

                        return true;
                    }
                    else 
                    {
                        //404
                        return false;
                    }
                }
            }



            return false;
        }
    }
}
