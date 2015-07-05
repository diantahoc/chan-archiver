using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json;

namespace ChanArchiver.HttpServerHandlers.JsonApi
{
    public sealed class GetDailyNetworkStatisticsJsonApiHandler
        : JsonApiHandlerBase
    {
        public const string Url = "/jsonapi/get-network-stats-day.json";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath.StartsWith(Url))
            {
                string day_number = request.QueryString[UrlParameters.DayNumber].Value;

                int dayNumber = 0;

                DateTime day = DateTime.Now;

                if (Int32.TryParse(day_number, out dayNumber))
                {
                    day = new DateTime(day.Year, 1, 1);

                    dayNumber--;

                    if (!DateTime.IsLeapYear(day.Year) && dayNumber == 365)
                    {
                        dayNumber--;
                    }

                    day = day.AddDays(dayNumber);
                }

                var sdata = NetworkUsageCounter.GetDayStats(day);

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
            return string.Format("{0}?{1}={2}", Url, UrlParameters.DayNumber, day.DayOfYear);
        }
    }
}
