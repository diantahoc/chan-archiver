using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver.HttpServerHandlers
{
    public class OverviewPageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command == "/")
            {

                StringBuilder sb = new StringBuilder(Properties.Resources.dashboard_page);

                sb.Replace("{thumbmode}", Settings.ThumbnailOnly ? "<div class=\"bs-callout bs-callout-warning\"><h4>ChanArchiver is running in thumbnail mode</h4><p>Only thumbnails will be saved. To disable it, restart ChanArchiver without the <code>--thumbonly</code> switch, or click <a href='/action/enablefullfile'>here</a></p></div>" : "");

                if (FileSystemStats.IsSaveDirDriveLowOnDiskSpace)
                {
                    sb.Replace("{lowdiskspacenotice}", "<div class=\"bs-callout bs-callout-danger\"><h4>Low disk space</h4><p>ChanArchive is configured to save on a harddrive with low disk space (less than 1 GB). Free up some disk space or change the save directory location.</p></div>");
                }
                else
                {
                    sb.Replace("{lowdiskspacenotice}", "");
                }

                sb.Replace("{RunningTime}", get_running_time_info());

                if (Settings.EnableFileStats)
                {
                    string disk_usage_table = "<h2 class=\"sub-header\">Disk usage</h2> <div class=\"table-responsive\"> <table class=\"table table-striped\"> <thead> <tr> <th># of files</th> <th>Category</th> <th>Used space</th> <th>Average file size</th> <th>Biggest file</th> <th>Smallest file</th> </tr> </thead> <tbody> {0} </tbody> </table> </div>";

                    sb.Replace("{DiskUsage}", string.Format(disk_usage_table, get_DiskUsageInfo()));
                }
                else
                {
                    sb.Replace("{DiskUsage}", "");
                }

                sb.Replace("{NetworkStats}", get_NetWorkStats());

                sb.Replace("{ArchivedThreads}", get_ArchivedThreadsStats());

                sb.Replace("{network-stats-data}", get_network_history(DateTime.Now));

                sb.Replace("{thread-stats-data}", get_json_thread_stats());

                sb.Replace("{network-stats-month}", get_network_history_month(DateTime.Now));

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }


            return false;
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

            DirectoryInfo board_storage = new DirectoryInfo(Program.post_files_dir);

            foreach (DirectoryInfo board_info in board_storage.GetDirectories())
            {
                sb.Append("<tr>");
                sb.AppendFormat("<td>/{0}/</td>", board_info.Name);
                sb.AppendFormat("<td>{0}</td>", board_info.GetDirectories().Length);
                sb.Append("</tr>");
            }

            return sb.ToString();
        }

        public string get_json_thread_stats() 
        {
            StringBuilder sb = new StringBuilder();

            DirectoryInfo board_storage = new DirectoryInfo(Program.post_files_dir);

            DirectoryInfo[] data = board_storage.GetDirectories();

            for (int i = 0; i < data.Length; i++) 
            {
                sb.AppendFormat("[ \"{0}\"  , {1} ]", data[i].Name, data[i].EnumerateDirectories().Count());

                if (i < data.Length - 1) 
                {
                    sb.Append(",");
                }
            }

            return sb.ToString();
        }

        private string get_network_history(DateTime day)
        {
            //[ ["January", 10], ["February", 8], ["March", 4], ["April", 13], ["May", 17], ["June", 9] ]
            StringBuilder sb = new StringBuilder();
            var data = NetworkUsageCounter.GetDayStats(day);
            for (int i = 0; i < data.Length; i++)
            {
                double t = data[i].Value / 1024 / 1024;
                sb.AppendFormat(" [ {0}, {1} ] ", data[i].Key, Math.Round(t, 2, MidpointRounding.AwayFromZero));
                if (i < data.Length - 1) { sb.Append(","); }
            }
            return sb.ToString();
        }

        private string get_network_history_month(DateTime day)
        {
            StringBuilder sb = new StringBuilder();
            var data = NetworkUsageCounter.GetMonthStats(day);
            for (int i = 0; i < data.Length; i++)
            {
                double t = data[i].Value / 1024 / 1024;
                sb.AppendFormat(" [ {0}, {1} ] ", data[i].Key, Math.Round(t, 2, MidpointRounding.AwayFromZero));
                if (i < data.Length - 1) { sb.Append(","); }
            }
            return sb.ToString();
        }

        private string get_running_time_info()
        {
            StringBuilder s = new StringBuilder();

            s.Append("<tr>");

            s.AppendFormat("<td>{0}</td>", HMSFormatter.GetReadableTimespan(DateTime.Now - Program.StartUpTime));

            s.AppendFormat("<td>{0}</td>", Settings.EnableFileStats ? Program.format_size_string(FileSystemStats.TotalUsage) : "<i class=\"fa fa-times-circle-o\"></i>");

            s.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.TotalConsumedAllTime));


            int archived_threads_count = 0;

            foreach (DirectoryInfo dir in (new DirectoryInfo(Program.post_files_dir)).GetDirectories())
            {
                archived_threads_count += dir.GetDirectories("*", SearchOption.TopDirectoryOnly).Length;
            }


            s.AppendFormat("<td>{0}</td>", archived_threads_count);

            s.Append("</tr>");

            return s.ToString();
        }
    }
}
