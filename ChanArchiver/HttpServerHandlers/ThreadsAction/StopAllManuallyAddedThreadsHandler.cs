using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChanArchiver.HttpServerHandlers.PageHandlers;

namespace ChanArchiver.HttpServerHandlers.ThreadsAction
{
    public sealed class StopAllManuallyAddedThreadsHandler
        : HttpServer.HttpModules.HttpModule
    {
        public const string Url = "/action/stopmanuallyaddedthreads/";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath.StartsWith(Url))
            {
                string board = request.QueryString[UrlParameters.Board].Value;

                if (!string.IsNullOrWhiteSpace(board))
                {
                    BoardWatcher bw = Program.GetBoardWatcher(board);

                    if (bw != null)
                    {
                        for (int i = 0; i < bw.watched_threads.Count; i++)
                        {
                            try
                            {
                                ThreadWorker tw = bw.watched_threads.ElementAt(i).Value;
                                if (!tw.AddedAutomatically)
                                {
                                    tw.Stop();
                                }
                            }
                            catch (System.IndexOutOfRangeException)
                            {
                                break;
                            }
                            catch { }
                        }
                    }
                }
                response.Redirect(MonitoredBoardsPageHandler.Url);
                return true;
            }

            return false;
        }

        public static string GetLinkToThisPage(string board) 
        {
            return string.Format("{0}?{1}={2}", Url, UrlParameters.Board, board);
        }
    }
}
