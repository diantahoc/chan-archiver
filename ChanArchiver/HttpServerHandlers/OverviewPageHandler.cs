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

                sb.Replace("{RunningTime}", (new RunningTimeInfo()).ToString());

                sb.Replace("{DiskUsage}", get_DiskUsageInfo());

                sb.Replace("{NetworkStats}", get_NetWorkStats());

                sb.Replace("{ArchivedThreads}", get_ArchivedThreadsStats());


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

        private static string get_NetWorkStats()
        {
            StringBuilder sb = new StringBuilder();


            double percent_api = NetworkUsageCounter.Total == 0 ? 0 : (NetworkUsageCounter.ApiConsumed / NetworkUsageCounter.Total) * 100;
            double percent_thumb = NetworkUsageCounter.Total == 0 ? 0 : (NetworkUsageCounter.ThumbConsumed / NetworkUsageCounter.Total) * 100;
            double percent_file = NetworkUsageCounter.Total == 0 ? 0 : (NetworkUsageCounter.FileConsumed / NetworkUsageCounter.Total) * 100;

            sb.Append("<tr>");

            sb.AppendFormat("<td>{0} %</td>", Math.Round(percent_api, MidpointRounding.ToEven));
            sb.AppendFormat("<td>{0} %</td>", Math.Round(percent_thumb, MidpointRounding.ToEven));
            sb.AppendFormat("<td>{0} %</td>", Math.Round(percent_file, MidpointRounding.ToEven));
            sb.Append("<td>-</td>");
            sb.Append("</tr>");

            sb.Append("<tr>");
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.ApiConsumed));
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.ThumbConsumed));
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.FileConsumed));
            sb.AppendFormat("<td>{0}</td>", Program.format_size_string(NetworkUsageCounter.Total));
            sb.Append("</tr>");

            return sb.ToString();
        }

        private static string get_DiskUsageInfo()
        {
            StringBuilder sb = new StringBuilder();

            KeyValuePair<string, string>[] dirs = new KeyValuePair<string, string>[] 
            {
                new KeyValuePair<string, string>("Thumbnails", Program.thumb_save_dir),
                new KeyValuePair<string, string>("Full Files", Program.file_save_dir),
                new KeyValuePair<string, string>("API Cached files", Program.api_cache_dir ),
                new KeyValuePair<string, string>("Post Files", Program.post_files_dir)
            };

            foreach (KeyValuePair<string, string> a in dirs)
            {
                DirectoryStats ds = new DirectoryStats(a.Value);

                sb.Append("<tr>");

                sb.AppendFormat("<td>{0}</td>", ds.FileCount);
                sb.AppendFormat("<td>{0}</td>", a.Key);
                sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.Size));
                sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.AverageFileSize));
                sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.BiggestFile));
                sb.AppendFormat("<td>{0}</td>", Program.format_size_string(ds.SmallestFile));

                sb.Append("</tr>");

            }

            return sb.ToString();
        }

        private static string get_ArchivedThreadsStats()
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
    }

    public class RunningTimeInfo
    {
        /*Running Time*/

        public RunningTimeInfo()
        {

            //rt
            this.RunningTime = DateTime.Now - Program.StartUpTime;


            //du
            DirectoryInfo di = new DirectoryInfo(Program.program_dir);

            foreach (FileInfo info in di.GetFiles("*", SearchOption.AllDirectories))
            {
                this.DiskUsage += info.Length;
            }

            //nu
            this.NetworkUsage = NetworkUsageCounter.Total;


            //ar
            DirectoryInfo board_dir = new DirectoryInfo(Program.post_files_dir);
            foreach (DirectoryInfo dir in board_dir.GetDirectories())
            {
                this.ArchivedThreads += dir.GetDirectories("*", SearchOption.TopDirectoryOnly).Length;
            }

            this.ApplicationErrors = 0;

        }

        public TimeSpan RunningTime { get; private set; }
        public long DiskUsage { get; private set; }
        public double NetworkUsage { get; private set; }
        public int ArchivedThreads { get; private set; }
        public int ApplicationErrors { get; private set; }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append("<tr>");

            s.AppendFormat("<td>{0}</td>", this.RunningTime.ToString());
            s.AppendFormat("<td>{0}</td>", Program.format_size_string(this.DiskUsage));
            s.AppendFormat("<td>{0}</td>", Program.format_size_string(this.NetworkUsage));
            s.AppendFormat("<td>{0}</td>", this.ArchivedThreads);
            s.AppendFormat("<td>{0}</td>", this.ApplicationErrors);

            s.Append("</tr>");

            return s.ToString();

        }

    }

    public class DirectoryStats
    {
        public DirectoryStats(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo df = new DirectoryInfo(path);

                FileInfo[] All_Files = df.GetFiles("*", SearchOption.AllDirectories);

                this.FileCount = All_Files.Length;

                IOrderedEnumerable<FileInfo> sorted = All_Files.OrderBy(x => x.Length); // ;_; poor poor poor performance

                for (int index = 0; index < this.FileCount; index++)
                {
                    FileInfo fifo = All_Files[index];
                    this.Size += fifo.Length;
                }

                if (sorted.Count() > 0)
                {
                    this.BiggestFile = sorted.Last().Length;
                    this.SmallestFile = sorted.First().Length;
                }
                else
                {
                    this.BiggestFile = 0;
                    this.SmallestFile = 0;
                }

                if (this.FileCount > 0)
                {
                    this.AverageFileSize = Convert.ToInt32(this.Size / this.FileCount);
                }
            }
        }

        public int FileCount { get; private set; }
        public long Size { get; private set; }
        public int AverageFileSize { get; private set; }
        public long BiggestFile { get; private set; }
        public long SmallestFile { get; private set; }



    }

}
