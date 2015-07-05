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
                        //data = Encoding.UTF8.GetBytes(Properties.Resources.bootstrap_css);
                        data = Properties.Resources.paper_theme_min;
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
                    case "jquery.flot.pie.min.js":
                        data = Properties.Resources.jquery_flot_pie_min;
                        response.ContentType = "application/javascript";
                        break;
                    case "sorttable.js":
                        data = Properties.Resources.sorttable_js;
                        response.ContentType = "application/javascript";
                        break;
                        //webfonts

                    case "font-awesome.min.css":
                        data = Properties.Resources.font_awesome_min;
                        response.ContentType = "text/css";
                        break;
                    case "fontawesome-webfont.eot":
                        data = Properties.Resources.fontawesome_webfont_eot;
                        response.ContentType = "application/vnd.ms-fontobject";
                        break;
                    case "fontawesome-webfont.svg":
                        data = Properties.Resources.fontawesome_webfont_svg;
                        response.ContentType = "image/svg+xml";
                        break;
                    case "fontawesome-webfont.ttf":
                    case "fontawesome-webfont.ttf?v=4.1.0":
                        data = Properties.Resources.fontawesome_webfont_ttf;
                        response.ContentType = "application/font-sfnt";
                        break;
                    case "fontawesome-webfont.woff":
                    case "fontawesome-webfont.woff?v=4.1.0":
                        data = Properties.Resources.fontawesome_webfont_woff;
                        response.ContentType = "application/font-woff";
                        break;
                    case "FontAwesome.otf":
                        data = Properties.Resources.FontAwesome_otf;
                        response.ContentType = "application/font-sfnt";
                        break;
                    case "verify.js":
                        data = Encoding.UTF8.GetBytes(Properties.Resources.verify_notify_min);
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
