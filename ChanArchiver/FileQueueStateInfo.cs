using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    public class FileQueueStateInfo
    {
        public string Hash { get; private set; }

        public double Length { get; set; }

        public double Downloaded { get; set; }

        public int RetryCount { get; set; }

        public enum DownloadStatus { Pending, Downloading, Error, Complete };

        public DownloadStatus Status { get; set; }

        public enum FileType { Thumbnail, FullFile };

        public FileType Type { get; set; }

        public string Url { get; set; }

        public double Percent()
        {
            if (this.Length > 0.0)
            {
                double progress = this.Downloaded / this.Length;

                //string s = string.Format("{0} %", Math.Round(progress * 100, 2));
                return progress * 100;
            }
            else
            {
                return 0;
            }
        }

        private List<LogEntry> mylogs = new List<LogEntry>();

        public LogEntry[] Logs { get { return this.mylogs.ToArray(); } }

        public void Log(LogEntry lo)
        {
            if (Program.verbose)
            {
                Program.PrintLog(lo);
            }
            this.mylogs.Add(lo);
        }


        public string Ext { get; private set; }
        public string FileName { get; private set; }

        public AniWrap.DataTypes.PostFile PostFile { get; private set; }

        public FileQueueStateInfo(string md5, AniWrap.DataTypes.PostFile pf)
        {
            this.Hash = md5;
            this.Ext = pf.ext;
            this.FileName = pf.filename;
            this.PostFile = pf;

            this.Status = DownloadStatus.Pending;
            this.RetryCount = 0;
        }
    }

}
