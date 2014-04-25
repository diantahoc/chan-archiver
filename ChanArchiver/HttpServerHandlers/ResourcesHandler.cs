﻿using System;
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

            if (command == "/favicon.ico")
            {
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentLength = Properties.Resources.favicon_ico.Length;
                response.ContentType = "image/x-icon";
                response.SendHeaders();
                response.SendBody(Properties.Resources.favicon_ico);
                return true;
            }

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
                    case "blue.css":
                        data = Encoding.UTF8.GetBytes(ChanArchiver.Properties.Resources.css_blue);
                        response.ContentType = "text/css";
                        break;
                    case "favicon.ico":
                        data = Properties.Resources.favicon_ico;
                        response.ContentType = "image/x-icon";
                        break;
                    case "jquery.flot.min.js":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.jquery_flot_min);
                        response.ContentType = "application/javascript";
                        break;
                    case "jquery.flot.categories.min.js":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.jquery_flot_categories_min);
                        response.ContentType = "application/javascript";
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
