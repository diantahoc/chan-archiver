using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.ThreadsAction
{
    public class ThreadNotesHandler
         : HttpServer.HttpModules.HttpModule
    {
        public const string Url = "/action/threadnotes/";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath.StartsWith(Url))
            {

                string board = request.QueryString[UrlParameters.Board].Value;
                string threadIdStr = request.QueryString[UrlParameters.ThreadId].Value;
                int threadId = -1;
                int.TryParse(threadIdStr, out threadId);

                if (!Program.IsBoardLetterValid(board))
                {
                    ThreadServerModule.write_text("Invalid board letter", response);
                    return true;
                }

                if (threadId <= 0)
                {
                    ThreadServerModule.write_text("Invalid thread id", response);
                    return true;
                }

                string notes = request.QueryString[UrlParameters.ThreadNotesText].Value;

                notes = System.Web.HttpUtility.HtmlDecode(notes);

                ThreadStore.GetStorageEngine().setThreadNotes(board, threadId, notes);

                response.Redirect(ThreadServerModule.GetThreadPageLink(board, threadId));

                return true;
            }
            return false;
        }

        public static string GetLinkToThisPage(string board, int threadid)
        {
            return string.Format("{0}?{1}={2}&{3}={4}", Url, UrlParameters.Board, board, UrlParameters.ThreadId, threadid.ToString());
        }
    }
}
