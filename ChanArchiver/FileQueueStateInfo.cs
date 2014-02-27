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

        public enum DownloadStatus { Pending, Downloading, Error };

        public DownloadStatus Status { get; set; }

        public enum FileType { Thumbnail, FullFile };

        public FileType Type { get; set; }

        public string Url { get; set; }

        public string Percent()
        {
            if (this.Length == 0)
            {
                return "0 %";
            }
            else
            {
                return string.Format("{0} %", Math.Round(Convert.ToDouble(Downloaded) / Convert.ToDouble(Length), MidpointRounding.AwayFromZero) * 100);
            }
        }

        public FileQueueStateInfo(string hash)
        {
            this.Hash = hash;

            this.Status = DownloadStatus.Pending;
            this.RetryCount = 0;
        }
    }

}
