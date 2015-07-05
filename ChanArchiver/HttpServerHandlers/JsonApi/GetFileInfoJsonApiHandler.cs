using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json;

namespace ChanArchiver.HttpServerHandlers.JsonApi
{
    public sealed class GetFileInfoJsonApiHandler
          : JsonApiHandlerBase
    {
        public const string Url = "/jsonapi/get-file-info.json";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath.StartsWith(Url))
            {
                string hash = request.QueryString["hash"].Value;

                if (string.IsNullOrEmpty(hash))
                {
                    ThreadServerModule._404(response);
                }
                else
                {

                    if (Program.queued_files.ContainsKey(hash))
                    {
                        FileQueueStateInfo f = Program.queued_files[hash];

                        JsonObject ob = new JsonObject();

                        ob.Add("p", f.Percent().ToString());
                        ob.Add("s", string.Format("{0} / {1}", Program.format_size_string(f.Downloaded), Program.format_size_string(f.Length)));
                        ob.Add("c", f.Status == FileQueueStateInfo.DownloadStatus.Complete);

                        WriteJsonResponse(response, ob.ToString());
                    }
                    else
                    {
                        ThreadServerModule._404(response);
                    }
                }
                return true;
            }

            return false;
        }

        public static string GetLinkToThisPage()
        {
            return Url;
        }
    }
}
