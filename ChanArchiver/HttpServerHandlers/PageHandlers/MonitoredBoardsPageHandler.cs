using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChanArchiver.HttpServerHandlers.ThreadsAction;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public sealed class MonitoredBoardsPageHandler
        : PageHandlerBase
    {
        public const string Url = "/monboards";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath == Url || request.UriPath == (Url + "/"))
            {
                StringBuilder sb = new StringBuilder(HtmlTemplates.MonitoredBoardsPageTemplate);

                IncludeCommonHtml(sb);

                sb.Replace("{items}", GetMonitoredBoardsTableHtml());

                sb.Replace("{blist}", ThreadServerModule.get_board_list("boardletter"));

                WriteFinalHtmlResponse(response, sb.ToString());

                return true;
            }

            return false;
        }

        public override string GetPageTitle()
        {
            return "Monitored Boards";
        }

        public override PageHandlerBase.PageType GetPageType()
        {
            return PageType.MonitoredBoards;
        }

        private string GetMonitoredBoardsTableHtml()
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
                        //sb.AppendFormat("<td><a class=\"btn btn-info\" href=\"/cancel/bwr/{0}\">Start (Full Mode)</a></td>", bw.Board);
                        sb.Append("<td>-</td>");
                    }

                    sb.AppendFormat("<td>/{0}/</td>", bw.Board);

                    sb.AppendFormat("<td>{0}</td>", bw.Mode.ToString());

                    sb.AppendFormat("<td>{0}</td>", bw.ActiveThreadWorkers);

                    sb.AppendFormat("<td><a class=\"btn btn-default\" href=\"{0}\">Stop all manually added threads</a></td>",
                        StopAllManuallyAddedThreadsHandler.GetLinkToThisPage(bw.Board));

                    sb.AppendFormat("<td><a href='/boards/{0}' class='label label-danger'>*click*</a></td>", bw.Board);

                    sb.AppendFormat("<td> <a href='/logs/{0}/{1}' class='label label-primary'>Logs</a> </td>", "boardwatcher", bw.Board);

                    sb.Append("</tr>");
                }
                catch (Exception)
                {
                    if (i >= Program.active_dumpers.Count()) { break; }
                }
            }

            return sb.ToString();
        }
    }
}
