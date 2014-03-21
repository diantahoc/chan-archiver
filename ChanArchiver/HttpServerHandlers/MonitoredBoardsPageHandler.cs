using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class MonitoredBoardsPageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command == "/monboards")
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < Program.active_dumpers.Count; i++)
                {
                    try
                    {
                        BoardWatcher bw = Program.active_dumpers.ElementAt(i).Value;

                        sb.Append("<tr>");

                        if (bw.IsMonitoring) //is running
                        {
                            sb.AppendFormat("<td><a class=\"btn btn-warning\" href=\"/cancel/bw/{0}\">Stop</a></td>", bw.Board);
                        }
                        else
                        {
                            sb.AppendFormat("<td><a class=\"btn btn-info\" href=\"/cancel/bwr/{0}\">Start (Full Mode)</a></td>", bw.Board);
                        }

                        sb.AppendFormat("<td>{0}</td>", string.Format("/{0}/", bw.Board));

                        sb.AppendFormat("<td>{0}</td>", bw.Mode.ToString());

                        sb.AppendFormat("<td>{0}</td>", bw.ActiveThreadWorkers);

                        sb.AppendFormat("<td><a href='/all/{0}' class='label label-danger'>*click*</a></td>", bw.Board);

                        sb.AppendFormat("<td> <a href='/logs/{0}/{1}' class='label label-primary'>Logs</a> </td>", "boardwatcher", bw.Board);

                        sb.Append("</tr>");
                    }
                    catch (Exception)
                    {
                        if (i >= Program.active_dumpers.Count) { break; }
                    }
                }

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.boards_page.Replace("{Items}", sb.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            return false;
        }
    }
}
