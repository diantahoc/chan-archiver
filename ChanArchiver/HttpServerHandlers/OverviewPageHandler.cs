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

                sb.Replace("{thumbmode}", Program.thumb_only ? "<div class=\"bs-callout bs-callout-warning\"><h4>ChanArchiver is running in thumbnail mode</h4><p>Only thumbnails will be saved. To disable it, restart ChanArchiver without the <code>--thumbonly</code> switch, or click <a href='/action/enablefullfile'>here</a></p></div>" : "");

                if (FileSystemStats.IsSaveDirDriveLowOnDiskSpace)
                {
                    sb.Replace("{lowdiskspacenotice}", "<div class=\"bs-callout bs-callout-danger\"><h4>Low disk space</h4><p>ChanArchive is configured to save on a harddrive with low disk space (less than 1 GB). Free up some disk space or change the save directory location.</p></div>");
                }
                else
                {
                    sb.Replace("{lowdiskspacenotice}", "");
                }

                sb.Replace("{RunningTime}", (new RunningTimeInfo()).ToString());

                sb.Replace("{DiskUsage}", get_DiskUsageInfo());

                sb.Replace("{NetworkStats}", get_NetWorkStats());

                sb.Replace("{ArchivedThreads}", get_ArchivedThreadsStats());

                sb.Replace("{network-stats-data}", get_network_history(DateTime.Now));

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

        private string get_network_history(DateTime day)
        {
            //[ ["January", 10], ["February", 8], ["March", 4], ["April", 13], ["May", 17], ["June", 9] ]
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            var data = NetworkUsageCounter.GetDayStats(day);
            for (int i = 0; i < data.Length; i++)
            {
                double t = data[i].Value / 1024 / 1024;
                sb.AppendFormat(" [ {0}, {1} ] ", data[i].Key, Math.Round(t, 2, MidpointRounding.AwayFromZero));
                if (i < data.Length - 1) { sb.Append(","); }
            }
            sb.Append("]");
            return sb.ToString();
        }
    }



    public class RunningTimeInfo
    {
        /*Running Time*/

        public RunningTimeInfo()
        {


            this.RunningTime = DateTime.Now - Program.StartUpTime;

            this.DiskUsage = FileSystemStats.TotalUsage;

        
            this.NetworkUsage = NetworkUsageCounter.TotalConsumedAllTime;


            //ar
            DirectoryInfo board_dir = new DirectoryInfo(Program.post_files_dir);
            foreach (DirectoryInfo dir in board_dir.GetDirectories())
            {
                this.ArchivedThreads += dir.GetDirectories("*", SearchOption.TopDirectoryOnly).Length;
            }

        }

        public TimeSpan RunningTime { get; private set; }
        public double DiskUsage { get; private set; }
        public double NetworkUsage { get; private set; }
        public int ArchivedThreads { get; private set; }
        //public int ApplicationErrors { get; private set; }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append("<tr>");

            s.AppendFormat("<td>{0}</td>", GetReadableTimespan(this.RunningTime));
            s.AppendFormat("<td>{0}</td>", Program.format_size_string(this.DiskUsage));
            s.AppendFormat("<td>{0}</td>", Program.format_size_string(this.NetworkUsage));
            s.AppendFormat("<td>{0}</td>", this.ArchivedThreads);
            //s.AppendFormat("<td>{0}</td>", this.ApplicationErrors);

            s.Append("</tr>");

            return s.ToString();

        }

        //http://stackoverflow.com/questions/16689468/how-to-produce-human-readable-strings-to-represent-a-timespan
        private string GetReadableTimespan(TimeSpan ts)
        {
            // formats and its cutoffs based on totalseconds
            var cutoff = new SortedList<long, string> 
            { 
                {60, "{3:S}" },
                {60*60, "{2:M}, {3:S}"},
                {24*60*60, "{1:H}, {2:M}"},
                {Int64.MaxValue , "{0:D}, {1:H}"}
            };

            // find nearest best match
            var find = cutoff.Keys.ToList()
                          .BinarySearch((long)ts.TotalSeconds);
            // negative values indicate a nearest match
            var near = find < 0 ? Math.Abs(find) - 1 : find;
            // use custom formatter to get the string
            return String.Format(
                new HMSFormatter(),
                cutoff[cutoff.Keys[near]],
                ts.Days,
                ts.Hours,
                ts.Minutes,
                ts.Seconds);
        }

    }


    // formatter for plural/singular forms of
    // seconds/hours/days
    //http://stackoverflow.com/questions/16689468/how-to-produce-human-readable-strings-to-represent-a-timespan
    public class HMSFormatter : ICustomFormatter, IFormatProvider
    {
        string _plural, _singular;

        public HMSFormatter() { }

        private HMSFormatter(string plural, string singular)
        {
            _plural = plural;
            _singular = singular;
        }

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg != null)
            {
                string fmt;
                switch (format)
                {
                    case "S": // second
                        fmt = String.Format(new HMSFormatter("{0} Seconds", "{0} Second"), "{0}", arg);
                        break;
                    case "M": // minute
                        fmt = String.Format(new HMSFormatter("{0} Minutes", "{0} Minute"), "{0}", arg);
                        break;
                    case "H": // hour
                        fmt = String.Format(new HMSFormatter("{0} Hours", "{0} Hour"), "{0}", arg);
                        break;
                    case "D": // day
                        fmt = String.Format(new HMSFormatter("{0} Days", "{0} Day"), "{0}", arg);
                        break;
                    default:
                        // plural/ singular             
                        fmt = String.Format((int)arg > 1 ? _plural : _singular, arg);  // watch the cast to int here...
                        break;
                }
                return fmt;
            }
            return String.Format(format, arg);
        }
    }

}
