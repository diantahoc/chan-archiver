using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public class WatchJobsPageHandler
        : PageHandlerBase
    {
        public const string Url = "/wjobs";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath == Url || request.UriPath == (Url + "/"))
            {
                StringBuilder sb = new StringBuilder(HtmlTemplates.WatchJobsPageTemplate);

                IncludeCommonHtml(sb);

                sb.Replace("{{watched-threads-table}}", GetWatchedThreadsTableHtml());

                sb.Replace("//{ai_items_js}", get_ai_items());

                sb.Replace("//{board_names_js}", get_boards_names_js());

                sb.Replace("{archives}", get_archives_html());

                WriteFinalHtmlResponse(response, sb.ToString());
                return true;
            }
            return false;
        }

        public override string GetPageTitle()
        {
            return "Watch Jobs";
        }

        public override PageHandlerBase.PageType GetPageType()
        {
            return PageType.WatchJobs;
        }

        private string GetWatchedThreadsTableHtml()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Program.active_dumpers.Count; i++)
            {
                try
                {
                    BoardWatcher bw = Program.active_dumpers.ElementAt(i).Value;

                    for (int index = 0; index < bw.watched_threads.Count; index++)
                    {
                        try
                        {
                            ThreadWorker tw = bw.watched_threads.ElementAt(index).Value;

                            //don't pollute the list with unactive thread workers (because no filter match) in monitor only mode
                            if (tw.AddedAutomatically && (!tw.IsActive) && bw.Mode == BoardWatcher.BoardMode.Monitor) { continue; }

                            sb.Append("<tr>");

                            sb.AppendFormat("<td><a class=\"btn btn-default\" href='/action/removethreadworker/?board={0}&id={1}' title='Remove'><i class=\"fa fa-trash-o\"></i></a></td>", tw.Board.Board, tw.ID);

                            if (tw.IsStatic)
                            {
                                sb.Append("<td><a class=\"btn btn-primary\" href='#'>Static</a></td>");
                            }
                            else
                            {
                                if (tw.IsActive)
                                {
                                    sb.AppendFormat("<td><a class=\"btn btn-warning\" href=\"/cancel/tw/{0}/{1}\">Stop</a></td>", bw.Board, tw.ID);
                                }
                                else
                                {
                                    sb.AppendFormat("<td><a class=\"btn btn-info\" href=\"/cancel/twr/{0}/{1}\">Start</a></td>", bw.Board, tw.ID);
                                }
                            }
                            sb.AppendFormat("<td>{0}</td>", string.Format("/{0}/", bw.Board));
                            sb.AppendFormat("<td>{0}</td>", tw.ID);

                            sb.AppendFormat("<td>{0}</td>", tw.AddedAutomatically ? "<span class=\"label label-primary\">Yes</span>" : "<span class=\"label label-default\">No</span>");

                            sb.AppendFormat("<td>{0}</td>", tw.ThumbOnly ? "<span class=\"label label-primary\">Yes</span>" : "<span class=\"label label-default\">No</span>");

                            sb.AppendFormat("<td><a href='/boards/{0}/{1}' class='label label-danger'>*click*</a></td>", bw.Board, tw.ID);

                            sb.AppendFormat("<td><pre>{0}</pre></td>", tw.ThreadTitle);

                            sb.AppendFormat("<td>{0} ago</td>", HMSFormatter.GetReadableTimespan(DateTime.Now - tw.LastUpdated).ToLower());

                            sb.AppendFormat("<td>{0}</td>", tw.AutoSage ? "<span class=\"label label-primary\">Yes</span>" : "<span class=\"label label-default\">No</span>");
                            sb.AppendFormat("<td>{0}</td>", tw.ImageLimitReached ? "<span class=\"label label-primary\">Yes</span>" : "<span class=\"label label-default\">No</span>");

                            sb.AppendFormat("<td> <a href='/threadinfo?board={0}&id={1}' class='label label-primary'>Info</a> </td>", bw.Board, tw.ID);

                            sb.Append("</tr>");
                        }
                        catch (Exception)
                        {
                            if (index >= bw.watched_threads.Count) { break; }
                        }
                    }

                }
                catch (Exception)
                {
                    if (i >= Program.active_dumpers.Count) { break; }
                }
            }
            return sb.ToString();
        }


        private string get_ai_items()
        {
            StringBuilder sb = new StringBuilder();

            /*

                {
                       supported_boards: [""],
                       supported_files: [""],
                       name: "",
                       ishttp: true,
                       ishttps: false
                 }
                  */

            int a_c = ArchivesProvider.GetAllArchives().Count();
            int w_ac = 0;

            foreach (ArchiveInfo w in ArchivesProvider.GetAllArchives())
            {
                sb.Append("{");
                sb.Append("supported_boards:[");

                int c = w.GetSupportedBoards().Count();
                int c_w = 0;
                foreach (string b in w.GetSupportedBoards())
                {
                    sb.Append("\"");
                    sb.Append(b);
                    sb.Append("\"");
                    if (c_w < c - 1)
                    {
                        sb.Append(",");
                    }
                    c_w++;
                }

                sb.Append("],");

                sb.Append("supported_files:[");

                c = w.GetSupportedFiles().Count();
                c_w = 0;
                foreach (string b in w.GetSupportedFiles())
                {
                    sb.Append("\"");
                    sb.Append(b);
                    sb.Append("\"");
                    if (c_w < c - 1)
                    {
                        sb.Append(",");
                    }
                    c_w++;
                }

                sb.Append("],");

                sb.Append("name: \"");
                sb.Append(w.Name.Replace("\"", @"\"""));
                sb.Append("\",");

                sb.Append("ishttp: ");
                sb.Append(w.SupportHttp ? "true" : "false");
                sb.Append(",");

                sb.Append("ishttps: ");
                sb.Append(w.SupportHttps ? "true" : "false");


                sb.Append("}");

                if (w_ac < a_c - 1)
                {
                    sb.Append(",");
                }
            }

            return sb.ToString();


        }

        private string get_boards_names_js()
        {
            StringBuilder sb = new StringBuilder();

            int c = 0;
            foreach (var board in Program.ValidBoards)
            {
                sb.AppendFormat(" {0}: \"{1}\"", board.Key, board.Value.Title.Replace("\"", @"\"""));
                if (c < Program.ValidBoards.Count - 1)
                {
                    sb.Append(",");
                }
            }

            return sb.ToString();
        }

        public string get_archives_html()
        {
            StringBuilder sb = new StringBuilder();
            int c = 0;

            foreach (var a in ArchivesProvider.GetAllArchives())
            {
                sb.AppendFormat("<option value='{0}'>{1}</option>", c, System.Web.HttpUtility.HtmlEncode(a.Name));
                c++;
            }

            return sb.ToString();
        }

    }
}
