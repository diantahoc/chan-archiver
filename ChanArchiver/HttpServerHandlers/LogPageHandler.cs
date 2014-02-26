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

            if (command == "/logs")
            {


                StringBuilder items = new StringBuilder();

                for (int index = 0; index < Program.logs.Count; index++)
                {

                    try
                    {
                        LogEntry e = Program.logs[index];

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
                    catch (Exception)
                    {
                        if (index >= Program.logs.Count) { break; }
                    }

                }

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.logs_page.Replace("{Logs}", items.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            return false;
        }
    }
}
