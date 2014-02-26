using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class ResourcesHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command.StartsWith("/res/"))
            {
                byte[] data = null;
                switch (command.Split('/')[2].ToLower())
                {
                    case "bgwhite.png":
                        data = Properties.Resources.bgwhite;
                        response.ContentType = "image/png";
                        break;
                    case "hr.png":
                        data = Properties.Resources.hr;
                        response.ContentType = "image/png";
                        break;
                    case "locked.png":
                        data = Properties.Resources.locked;
                        response.ContentType = "image/png";
                        break;
                    case "sticky.png":
                        data = Properties.Resources.sticky;
                        response.ContentType = "image/png";
                        break;
                    case "bootstrap.css":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.bootstrap_css);
                        response.ContentType = "text/css";
                        break;
                    case "dashboard.css":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.dashboard_css);
                        response.ContentType = "text/css";
                        break;
                    case "bootstrap.js":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.bootstrap_js);
                        response.ContentType = "application/javascript";
                        break;
                    case "jquery.js":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.jquery_js);
                        response.ContentType = "application/javascript";
                        break;
                    case "docs.js":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.docs_js);
                        response.ContentType = "application/javascript";
                        break;
                    case "css.css":
                        data = Encoding.UTF8.GetBytes(ChanArchiver.Properties.Resources.layout);  
                        response.ContentType = "text/css";
                        break;
                    default:
                        break;
                }

                if (data == null)
                {
                    ThreadServerModule._404(response);
                }
                else
                {
                    response.Status = System.Net.HttpStatusCode.OK;
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);
                }
            }

            return false;
        }
    }
}
