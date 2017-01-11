using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json;
using ChanArchiver.Thread_Storage;

namespace ChanArchiver.HttpServerHandlers.JsonApi
{
    public sealed class GetThreadsStatisticsJsonApiHandler
          : JsonApiHandlerBase
    {
        public const string Url = "/jsonapi/get-threads-statistics.json";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath == Url)
            {
                ThreadStore.GetStorageEngine().UpdateThreadStoreStats();
                ThreadStoreStats stats = ThreadStore.GetStorageEngine().StoreStats;

                JsonArray ja = new JsonArray();

                IEnumerable<string> boards = Program.ValidBoards.Keys;

                for (int i = 0, j = boards.Count(); i < j; i++)
                {
                    string boardName = boards.ElementAt(i);
                    int threadCount = stats[boardName];

                    if (threadCount > 0)
                    {
                        JsonArray inner = new JsonArray();

                        inner.Add(boardName);
                        inner.Add(threadCount);

                        ja.Add(inner);
                    }
                }

                WriteJsonResponse(response, ja.ToString());
                return true;
            }

            return false;
        }

        public static string GetLinkToThisPage() { return Url; }
    }
}
