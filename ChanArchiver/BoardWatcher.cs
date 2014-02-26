using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AniWrap.DataTypes;
using System.Threading.Tasks;
namespace ChanArchiver
{
    public class BoardWatcher
    {
        public string Board { get; private set; }

        RSSWatcher rss_w;

        private List<int> _404_threads = new List<int>();

        public Dictionary<int, ThreadWorker> watched_threads = new Dictionary<int, ThreadWorker>();

        private bool _has_started = false;

        public int ThreadWorkerInterval { get { return 2; } }

        public bool IsFullMode { get { return _has_started; } }

        public BoardWatcher(string board)
        {
            if (string.IsNullOrEmpty(board))
            {
                throw new ArgumentNullException("Board letter cannot be null");
            }
            else
            {
                this.Board = board;
            }
        }


        public void StartFullMode()
        {
            //First we load the catalog data, then we start the RSS watcher.
            //The reason that we don't use the RSS watcher at first, because the RSS feed is limited to 
            //20 thread. So the RSS watcher is used to check for new threads instead of re-downloading the catalog
            if (!_has_started)
            {
                Task.Factory.StartNew((Action)delegate
                {
                    //log("Starting board watcher");
                    load_catalog();
                    catalog_loaded_callback();
                    // log("Board watcher started");
                });
            }
        }

        public void AddThreadId(int id)
        {
            if (!this.watched_threads.ContainsKey(id))
            {
                ThreadWorker t = new ThreadWorker(this, id);
                this.watched_threads.Add(id, t);
                t.Thread404 += this.handle_thread_404;
                t.Start();
            }
        }


        private void load_catalog()
        {
            _has_started = true;

            while (true)
            {
                try
                {

                    Program.LogMessage(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Info,
                        Message = "Downloading catalog data...",
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });

                    CatalogItem[][] catalog = Program.aw.GetCatalog(this.Board);


                    Program.LogMessage(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Success,
                        Message = "Catalog data downloaded",
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });

                    int loaded_count = 0;
                    foreach (CatalogItem[] page in catalog)
                    {
                        foreach (CatalogItem thread in page)
                        {
                            watched_threads.Add(thread.ID, new ThreadWorker(this, thread.ID));


                            Program.LogMessage(new LogEntry()
                            {
                                Level = LogEntry.LogLevel.Info,
                                Message = string.Format("Loaded from catalog thread {0}", thread.ID),
                                Sender = "BoardWatcher",
                                Title = string.Format("/{0}/", this.Board)
                            });


                            loaded_count++;
                        }
                    }

                    Program.LogMessage(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Success,
                        Message = string.Format("Finished loading {0} thread", loaded_count),
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });

                    break;

                }
                catch (Exception)
                {

                    Program.LogMessage(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Fail,
                        Message = "Could not load catalog data, retrying in 5 seconds...",
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });

                    System.Threading.Thread.Sleep(5000);
                }
            }
            return;
        }

        public void Stop()
        {
            for (int i = 0; i < this.watched_threads.Count; i++)
            {
                try
                {
                    ThreadWorker tw = this.watched_threads.ElementAt(i).Value;

                    if (tw.StartedByBW) { tw.Stop(); }

                }
                catch (Exception)
                {
                    if (i >= this.watched_threads.Count) { break; }
                }
            }
            if (this.rss_w != null) { this.rss_w.Stop(); }
            this._has_started = false;
        }

        private void catalog_loaded_callback()
        {
            foreach (KeyValuePair<int, ThreadWorker> w in watched_threads)
            {
                w.Value.Thread404 += this.handle_thread_404;
                w.Value.StartedByBW = true;
                w.Value.Start();

                Program.LogMessage(new LogEntry()
                {
                    Level = LogEntry.LogLevel.Info,
                    Message = " Starting thread worker '" + w.Value.ID + "'",
                    Sender = "BoardWatcher",
                    Title = string.Format("/{0}/", this.Board)
                });

                System.Threading.Thread.Sleep(1500); //Allow 1.5 seconds between thread worker startup
            }

            start_rss_watcher();
        }

        private void handle_thread_404(ThreadWorker instance)
        {
            this.watched_threads.Remove(instance.ID);
            this._404_threads.Add(instance.ID);
            instance.Dispose();
            instance.Thread404 -= this.handle_thread_404;

            Program.LogMessage(new LogEntry()
            {
                Level = LogEntry.LogLevel.Info,
                Message = string.Format("Thread '{0}' has 404'ed", instance.ID),
                Sender = "BoardWatcher",
                Title = string.Format("/{0}/", this.Board)
            });

        }

        private void start_rss_watcher()
        {
            rss_w = new RSSWatcher(this);
            rss_w.DataRefreshed += this.handle_rss_watcher_newdata;
            rss_w.Start();

            Program.LogMessage(new LogEntry()
            {
                Level = LogEntry.LogLevel.Success,
                Message = "RSS Watcher has started",
                Sender = "RSSWatcher",
                Title = string.Format("/{0}/", this.Board)
            });
        }

        private void handle_rss_watcher_newdata(int[] data)
        {
            foreach (int id in data)
            {
                if (!this.watched_threads.ContainsKey(id))
                {
                    if (!this._404_threads.Contains(id))
                    {
                        ThreadWorker t = new ThreadWorker(this, id);
                        this.watched_threads.Add(id, t);
                        t.Thread404 += this.handle_thread_404;
                        t.StartedByBW = true;
                        t.Start();
                        Program.LogMessage(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Info,
                            Message = string.Format("Found new thread {0}", id),
                            Sender = "RSSWatcher",
                            Title = string.Format("/{0}/", this.Board)
                        });
                    }
                }
            }
        }
    }
}
