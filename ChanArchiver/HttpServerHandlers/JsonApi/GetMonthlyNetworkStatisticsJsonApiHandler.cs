using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json;

namespace ChanArchiver.HttpServerHandlers.JsonApi
{
    public sealed class GetMonthlyNetworkStatisticsJsonApiHandler
        : JsonApiHandlerBase
    {
        public const string Url = "/jsonapi/get-network-stats-month.json";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath.StartsWith(Url))
            {
                string month_number = request.QueryString[UrlParameters.MonthNumber].Value;

                int monthNumber = 0;

                DateTime day = DateTime.Now;

                if (Int32.TryParse(month_number, out monthNumber))
                {
                    day = new DateTime(day.Year, 1, 1);

                    monthNumber--;

                    day = day.AddMonths(monthNumber);
                }

                var sdata = NetworkUsageCounter.GetMonthStats(day);

                JsonArray ja = new JsonArray();

                for (int i = 0; i < sdata.Length; i++)
                {
                    double t = sdata[i].Value / 1024 / 1024;

                    JsonArray inner = new JsonArray();

                    inner.Add(sdata[i].Key);
                    inner.Add(Math.Round(t, 2, MidpointRounding.AwayFromZero));

                    ja.Add(inner);
                }

                WriteJsonResponse(response, ja.ToString());
                return true;
            }

            return false;
        }

        public static string GetLinkToThisPage()
        {
            return GetLinkToThisPage(DateTime.Now);
        }

        public static string GetLinkToThisPage(DateTime day)
        {
            return string.Format("{0}?{1}={2}", Url, UrlParameters.MonthNumber, day.Month);
        }
    }
}
