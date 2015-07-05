using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ChanArchiver.HttpServerHandlers.SettingsAction;
using ChanArchiver.HttpServerHandlers.JsonApi;
using ChanArchiver.Thread_Storage;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public sealed class OverviewPageHandler
        : PageHandlerBase
    {
        public const string Url = "/";

        // TODO: Move these html notices to the html template
        private const string ThumbnailOnlyWarningMessageHtml = "<div class=\"bs-callout bs-callout-warning\"><h4>"
                        + "ChanArchiver is running in thumbnail mode</h4><p>Only thumbnails will be saved. "
                        + "To disable it, restart ChanArchiver without the <code>--thumbonly</code>" +
                        " switch, or click <a href=''>here</a></p></div>";

        private const string LowDiskSpaceWarningMessageHtml = "<div class=\"bs-callout bs-callout-danger\">"
                       + "<h4>Low disk space</h4><p>ChanArchive is configured to save on a harddrive with low disk space"
                       + " (less than 1 GB). Free up some disk space or change the save directory location.</p></div>";

        private const string DiskUsageTableHtml = "<h2 class=\"sub-header\">Disk usage</h2><div class=\"table-responsive\">"
            + "<table class=\"table table-striped\"><thead><tr> "
            + "<th># of files</th>"
            + "<th>Category</th>"
            + "<th>Used space</th>"
            + "<th>Average file size</th>"
            + "<th>Biggest file</th>"
            + "<th>Smallest file</th>"
            + "</tr></thead><tbody> {0} </tbody></table></div>";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath == Url)
            {
                ThreadStore.UpdateThreadStoreStats();

                StringBuilder sb = new StringBuilder(HtmlTemplates.OverviewPageTemplate);

                IncludeCommonHtml(sb);

                if (Settings.ThumbnailOnly)
                {
                    string url = ThumbnailOnlySettingsController.GetLinkToThisPage(true, Url);
                    sb.Replace("{thumbmode}", string.Format(ThumbnailOnlyWarningMessageHtml, url));
                }
                else
                {
                    sb.Replace("{thumbmode}", "");
                }

                if (FileSystemStats.IsSaveDirDriveLowOnDiskSpace)
                {
                    sb.Replace("{lowdiskspacenotice}", LowDiskSpaceWarningMessageHtml);
                }
                else
                {
                    sb.Replace("{lowdiskspacenotice}", "");
                }

                sb.Replace("{RunningTime}", get_running_time_info());

                if (Settings.EnableFileStats)
                {
                    sb.Replace("{DiskUsage}", string.Format(DiskUsageTableHtml, get_DiskUsageInfo()));
                }
                else
                {
                    sb.Replace("{DiskUsage}", "");
                }

                sb.Replace("{NetworkStats}", get_NetWorkStats());

                sb.Replace("{ArchivedThreads}", get_ArchivedThreadsStats());

                sb.Replace("{get-network-stats-today-api-link}",
                    GetDailyNetworkStatisticsJsonApiHandler.GetLinkToThisPage(DateTime.Now));

                sb.Replace("{get-network-stats-month-api-link}",
                    GetMonthlyNetworkStatisticsJsonApiHandler.GetLinkToThisPage(DateTime.Now));

                sb.Replace("{get-threads-stats-api-link}",
                    GetThreadsStatisticsJsonApiHandler.GetLinkToThisPage());

                WriteFinalHtmlResponse(response, sb.ToString());

                return true;
            }
            return false;
        }

        public override PageType GetPageType()
        {
            return PageType.Overview;
        }

        public override string GetPageTitle()
        {
            return "Overview";
        }

        private string get_NetWorkStats()
        {
            StringBuilder sb = new StringBuilder();

            double percent_api = NetworkUsageCounter.TotalThisHour == 0 ? 0 : (NetworkUsageCounter.ApiConsumedThisHour / NetworkUsageCounter.TotalThisHour) * 100;
            double percent_thumb = NetworkUsageCounter.TotalThisHour == 0 ? 0 : (NetworkUsageCounter.ThumbConsumedThisHour / NetworkUsageCounter.TotalThisHour) * 100;
            double percent_file = NetworkUsageCounter.TotalThisHour == 0 ? 0 : (NetworkUsageCounter.FileConsumedThisHour / NetworkUsageCounter.TotalThisHour) * 100;

            sb.Append("<tr>");

            sb.AppendFormat("<td>{0} %</td>", Math.Round(percent_api, MidpointRounding.ToEven));
            sb.AppendFormat("<td>{0} %</td>", Math.Round(percent_thumb, MidpointRounding.ToEven));
            sb.AppendFormat("<td>{0} %</td>", Math.Round(percent_file, MidpointRounding.ToEven));
            sb.Append("<td>-</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.ApiConsumedThisHour));
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.ThumbConsumedThisHour));
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.FileConsumedThisHour));
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.TotalThisHour));
            sb.Append("</tr>");

            return sb.ToString();
        }

        private string get_DiskUsageInfo()
        {
            StringBuilder sb = new StringBuilder();

            KeyValuePair<string, string>[] dirs = new KeyValuePair<string, string>[] 
            {
                new KeyValuePair<string, string>("Thumbnails", Program.thumb_save_dir),
                new KeyValuePair<string, string>("Full files", Program.file_save_dir),
                new KeyValuePair<string, string>("API Cached files", Program.api_cache_dir ),
                new KeyValuePair<string, string>("Post files", Program.post_files_dir),
               // new KeyValuePair<string, string>("Temporary files", Program.temp_files_dir),
            };

            foreach (KeyValuePair<string, string> a in dirs.OrderBy(x => x.Key))
            {
                DirectoryStatsEntry ds = FileSystemStats.GetDirStats(a.Value);

                if (ds != null)
                {
                    sb.Append("<tr>");
                    sb.AppendFormat("<td>{0}</td>", ds.FileCount);
                    sb.AppendFormat("<td>{0}</td>", a.Key);
                    sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.TotalSize));
                    sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.AverageFileSize));
                    sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.LargestFile));
                    sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.SmallestFile));
                    sb.Append("</tr>");
                }
            }

            return sb.ToString();
        }

        private string get_ArchivedThreadsStats()
        {
            StringBuilder sb = new StringBuilder();
            ThreadStoreStats stats = ThreadStore.StoreStats;

            foreach (string board in Program.ValidBoards.Keys)
            {
                int threadCount = stats[board];
                if (threadCount > 0)
                {
                    sb.Append("<tr>");
                    sb.AppendFormat("<td>/{0}/</td>", board);
                    sb.AppendFormat("<td>{0}</td>", threadCount);
                    sb.Append("</tr>");
                }
            }

            return sb.ToString();
        }

        private string get_running_time_info()
        {
            StringBuilder s = new StringBuilder();

            s.Append("<tr>");

            s.AppendFormat("<td>{0}</td>", HMSFormatter.GetReadableTimespan(DateTime.Now - Program.StartUpTime));

            if (Settings.EnableFileStats)
            {
                s.AppendFormat("<td>{0}</td>", Program.format_size_string(FileSystemStats.TotalUsage));
            }
            else
            {
                s.Append("<td><i class=\"fa fa-times-circle-o\"></i></td>");
            }

            s.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.TotalConsumedAllTime));

            s.AppendFormat("<td>{0}</td>", ThreadStore.StoreStats.TotalArchivedThreadsCount);

            s.Append("</tr>");

            return s.ToString();
        }
    }
}
