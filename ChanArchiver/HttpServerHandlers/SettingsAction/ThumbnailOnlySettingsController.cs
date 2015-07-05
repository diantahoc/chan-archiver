using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.SettingsAction
{
    public sealed class ThumbnailOnlySettingsController
        : HttpServer.HttpModules.HttpModule
    {
        public const string Url = "/action/settings/thumbnailonly/";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath.StartsWith(Url))
            {
                string value = request.QueryString[UrlParameters.IsEnabled].Value;
                Settings.ThumbnailOnly = value == "true";

                string redirect = request.QueryString[UrlParameters.RedirectUrl].Value;

                if (!string.IsNullOrWhiteSpace(redirect))
                {
                    response.Redirect(redirect);
                }
                else
                {
                    response.Redirect("/");
                }

                return true;
            }

            return false;
        }

        public static string GetLinkToThisPage(bool isEnabled)
        {
            return GetLinkToThisPage(isEnabled, "/");
        }

        public static string GetLinkToThisPage(bool isEnabled, string redirectUrl)
        {
            return string.Format("{0}?{1}={2}&{3}={4}", Url, UrlParameters.IsEnabled, isEnabled, UrlParameters.RedirectUrl, redirectUrl);
        }
    }
}
