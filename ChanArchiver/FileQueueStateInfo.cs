﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    /// <summary>
    /// Queued File Info
    /// </summary>
    public class FileQueueStateInfo
    {
        public string Hash { get; private set; }

        public double Length { get; set; }

        public double Downloaded { get; set; }

        public int RetryCount { get; set; }

        public enum DownloadStatus { Queued, Connecting, Downloading, Error, Complete, Stopped, NotFound };

        public DownloadStatus Status { get; set; }

        public bool IsThumbOnly { get; set; }

        public enum FileType { Thumbnail, FullFile };

        public FileType Type { get; set; }

        public string Url
        {
            get
            {
                if (this.Type == FileType.FullFile)
                {
                    return this.PostFile.FullImageLink;
                }
                else 
                {
                    return this.PostFile.ThumbLink;
                }
            }
        }

        public Amib.Threading.WorkItemPriority Priority { get; set; }

        public Amib.Threading.IWorkItemResult ThreadBG { get; set; }

        public int HashFailCount { get; set; }

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
            this.ForceStop = false;
            this.Hash = md5;
            this.Ext = pf.ext;
            this.FileName = pf.filename;
            this.PostFile = pf;

            this.Status = DownloadStatus.Queued;
            this.RetryCount = 0;
        }

        public bool ForceStop { get; set; }
    }

}
