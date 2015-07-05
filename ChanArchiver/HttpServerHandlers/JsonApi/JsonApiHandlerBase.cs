using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.JsonApi
{
    public abstract class JsonApiHandlerBase
          : HttpServer.HttpModules.HttpModule
    {
        protected void WriteJsonResponse(HttpServer.IHttpResponse response, string result) 
        {
            byte[] data = Encoding.UTF8.GetBytes(result);
            response.Status = System.Net.HttpStatusCode.OK;
            response.ContentType = ServerConstants.JsonContentType;
            response.ContentLength = data.Length;
            response.SendHeaders();
            response.SendBody(data);
        }
    }
}
