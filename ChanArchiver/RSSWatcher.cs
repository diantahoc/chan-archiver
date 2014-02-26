using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Rss;

namespace ChanArchiver
{
    public class RSSWatcher : IDisposable
    {
        private string board;
        private BackgroundWorker worker;

        private string link;

        public RSSWatcher(BoardWatcher board)
        {
            this.board = board.Board;
            this.link = "http://boards.4chan.org/$/index.rss".Replace("$", this.board.ToLower());
            this.worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            this.worker.DoWork += new DoWorkEventHandler(worker_DoWork);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                try
                {
                    RssFeed feed = RssFeed.Read(link);

                    List<int> ids = new List<int>();

                    foreach (RssChannel cha in feed.Channels)
                    {
                        foreach (RssItem i in cha.Items)
                        {
                            try
                            {
                                ids.Add(Convert.ToInt32(i.Guid.Name.Split('/').Last()));
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }

                    DataRefreshed(ids.ToArray());
                }
                catch (Exception) { }

                System.Threading.Thread.Sleep(90000); // sleep 1.5 min
            }
        }

        public delegate void DataRefreshedEvent(int[] list);
        public event DataRefreshedEvent DataRefreshed;

        public void Start()
        {
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
        }

        public void Stop()
        {
            this.worker.CancelAsync();
        }

        public void Dispose()
        {
            this.worker.Dispose();
        }

    }
}
