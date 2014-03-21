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

        private RSSWatcher rss_w;
        private List<LogEntry> mylogs = new List<LogEntry>();

        private List<int> _404_threads = new List<int>();

        public Dictionary<int, ThreadWorker> watched_threads = new Dictionary<int, ThreadWorker>();

        public int ThreadWorkerInterval { get { return 2; } }

        public bool IsMonitoring { get { return this.Mode != BoardMode.None; } }

        public double AvgPostPerMinute { get; private set; }

        //public TimeSpan AvgThreadLifeTime { get; private set; }

        public LogEntry[] Logs { get { return this.mylogs.ToArray(); } }

        public enum BoardSpeed { Slow, Normal, Fast }

        public BoardSpeed Speed { get; private set; }

        public BoardMode Mode { get; private set; }


        public BoardWatcher(string board)
        {
            if (string.IsNullOrEmpty(board))
            {
                throw new ArgumentNullException("Board letter cannot be null");
            }
            else
            {
                this.Board = board;
                this.Mode = BoardMode.None;
                 LoadFilters();
                // Task.Factory.StartNew(load_board_sleep_times);
            }
        }

        public enum BoardMode
        {
            /// <summary>
            /// Do nothing. Act as a container for individual ThreadWorkers
            /// </summary>
            None,
            /// <summary>
            /// Monitor board for new threads and archive all new threads.
            /// </summary>
            FullBoard,
            /// <summary>
            /// Monitor board for new threads, and only archive threads who has a filter match
            /// </summary>
            Monitor
        }

        //private void load_board_sleep_times()
        //{
        //    try
        //    {
        //        AniWrap.AniWrap.ThreadAndDate[] e = Program.aw.GetBoardThreadsID(this.Board);
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        public void StartMonitoring(BoardMode mode)
        {
            if (mode == BoardMode.None) { return; }
            //First we load the catalog data, then we start the RSS watcher.
            //The reason that we don't use the RSS watcher at first, because the RSS feed is limited to 
            //20 thread. So the RSS watcher is used to check for new threads instead of re-downloading the catalog
            if (this.Mode == BoardMode.None /*|| this.Mode != mode*/)
            {
                this.Mode = mode;

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

        public int ActiveThreadWorkers 
        {
            get 
            {
                int act=0;

                int[] keys = this.watched_threads.Keys.ToArray();

                foreach (int id in keys)
                {
                    try
                    {
                        if (this.watched_threads.ContainsKey(id))
                        {
                            if (this.watched_threads[id].IsActive) { act++; }
                        }
                    }
                    catch (Exception) { }
                }
                return act;
            }
        }

        private void load_catalog()
        {
            while (true)
            {
                try
                {
                    List<DateTime> post_dates = new List<DateTime>();

                    List<DateTime> op_post_dates = new List<DateTime>();

                    Log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Info,
                        Message = "Downloading catalog data...",
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });

                    CatalogItem[][] catalog = Program.aw.GetCatalog(this.Board);


                    Log(new LogEntry()
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
                            if (this.Mode == BoardMode.Monitor) 
                            {
                                if (!this.MatchFilters(thread)) 
                                {
                                    //in monitor mode, don't add unacceptable threads
                                    continue;
                                }
                            }

                            if (!watched_threads.ContainsKey(thread.ID)) 
                            {
                                watched_threads.Add(thread.ID, new ThreadWorker(this, thread.ID)
                                {
                                    ImageLimit = thread.ImageLimit,
                                    BumpLimit = thread.BumpLimit,
                                    AddedAutomatically = true
                                });
                            }

                            if (Program.verbose)
                            {
                                Log(new LogEntry()
                                {
                                    Level = LogEntry.LogLevel.Info,
                                    Message = string.Format("Loaded from catalog thread {0}", thread.ID),
                                    Sender = "BoardWatcher",
                                    Title = string.Format("/{0}/", this.Board)
                                });
                            }

                            if (thread.trails != null)
                            {
                                foreach (GenericPost p in thread.trails)
                                {
                                    post_dates.Add(p.Time);
                                }
                            }

                            op_post_dates.Add(thread.Time);

                            loaded_count++;
                        }
                    }

                    Log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Success,
                        Message = string.Format("Finished loading {0} thread", loaded_count),
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });


                    IOrderedEnumerable<DateTime> sorted_dates = post_dates.OrderBy(x => x);

                    TimeSpan ts = sorted_dates.Last() - sorted_dates.First();

                    this.AvgPostPerMinute = post_dates.Count / ts.TotalMinutes;

                    //IOrderedEnumerable<DateTime> sorted_op = op_post_dates.OrderBy(x => x);

                    //this.AvgThreadLifeTime = sorted_op.Last() - sorted_op.First();

                    /*
                       if the ppm < 1, it is a slow board.
                       if the 5 <= ppm, it is a fast board.
                       if (1 < ppm < 5), it is a normal board. 
                     */

                    if (this.AvgPostPerMinute < 1.0)
                    {
                        this.Speed = BoardSpeed.Slow;
                    }
                    else if (1.0 < this.AvgPostPerMinute && this.AvgPostPerMinute < 5.0)
                    {
                        this.Speed = BoardSpeed.Normal;
                    }
                    else
                    {
                        this.Speed = BoardSpeed.Fast;
                    }

                    //Log(new LogEntry()
                    //{
                    //    Level = LogEntry.LogLevel.Info,
                    //    Message = string.Format("Average thread lifetime: {0}", this.AvgThreadLifeTime),
                    //    Sender = "BoardWatcher",
                    //    Title = string.Format("/{0}/", this.Board)
                    //});

                    //Log(new LogEntry()
                    //{
                    //    Level = LogEntry.LogLevel.Info,
                    //    Message = string.Format("Average post per minute (ppm): {0}", this.AvgPostPerMinute),
                    //    Sender = "BoardWatcher",
                    //    Title = string.Format("/{0}/", this.Board)
                    //});

                    break;
                }
                catch (Exception ex)
                {
                    Log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Fail,
                        Message = string.Format("Could not load catalog data '{0}' @ '{1}', retrying in 5 seconds", ex.Message, ex.StackTrace),
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });

                    System.Threading.Thread.Sleep(5000);
                }
            }
            return;
        }

        public void StopMonitoring()
        {
            int[] keys = this.watched_threads.Keys.ToArray();

            foreach (int id in keys)
            {
                try
                {
                    if (this.watched_threads.ContainsKey(id))
                    {
                        ThreadWorker tw = this.watched_threads[id];

                        if (tw.AddedAutomatically)
                        {
                            tw.Stop();
                            this.watched_threads.Remove(id);
                        }
                    }
                }
                catch (Exception) { }
            }

            if (this.rss_w != null) { this.rss_w.Stop(); }

            this.Mode = BoardMode.None;
        }

        private void catalog_loaded_callback()
        {
            foreach (KeyValuePair<int, ThreadWorker> w in watched_threads)
            {
                if (w.Value.AddedAutomatically)
                {
                    w.Value.Thread404 += this.handle_thread_404;
                    w.Value.Start();

                    if (Program.verbose)
                    {
                        Log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Info,
                            Message = "Starting thread worker '" + w.Value.ID + "'",
                            Sender = "BoardWatcher",
                            Title = string.Format("/{0}/", this.Board)
                        });
                    }

                    System.Threading.Thread.Sleep(500); //Allow 0.5 seconds between thread worker startup
                }
            }
            start_rss_watcher();
        }

        private void handle_thread_404(ThreadWorker instance)
        {
            this.watched_threads.Remove(instance.ID);
            this._404_threads.Add(instance.ID);
            instance.Dispose();
            instance.Thread404 -= this.handle_thread_404;

            Log(new LogEntry()
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

            Log(new LogEntry()
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
                        t.AddedAutomatically = true;
                        this.watched_threads.Add(id, t);
                        t.Thread404 += this.handle_thread_404;
                      
                        t.Start();
                        Log(new LogEntry()
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

        List<ChanArchiver.Filters.IFilter> my_filters = new List<Filters.IFilter>();

        public ChanArchiver.Filters.IFilter[] Filters 
        {
            get { return this.my_filters.ToArray(); }
        } 

        public void AddFilter(ChanArchiver.Filters.IFilter filter)
        {
            if (filter != null)
            {
                my_filters.Add(filter);
            }
        }

        public void RemoveFilter(int index) 
        {
            this.my_filters.RemoveAt(index);
        }

        public bool MatchFilters(AniWrap.DataTypes.GenericPost post)
        {
            for (int i = 0; i < this.my_filters.Count(); i++)
            {
                try
                {
                    if (my_filters[i].Detect(post)) 
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        public void SaveFilters()
        {
            List<string[]> s = new List<string[]>();

            ChanArchiver.Filters.IFilter[] filters = my_filters.ToArray();

            foreach (ChanArchiver.Filters.IFilter filter in filters)
            {
                s.Add(new string[] { filter.GetType().FullName, filter.FilterText });
            }

            System.IO.File.WriteAllText(this.FilterSaveFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(s));
        }

        private string FilterSaveFilePath
        {
            get
            {
                return System.IO.Path.Combine(Program.board_settings_dir, string.Format("filters-{0}.json", this.Board));
            }
        }

        public void LoadFilters()
        {
            if (System.IO.File.Exists(this.FilterSaveFilePath))
            {
                List<object> s = (List<object>)Newtonsoft.Json.JsonConvert.DeserializeObject(System.IO.File.ReadAllText(this.FilterSaveFilePath), typeof(List<object>));

                foreach (object filter in s)
                {
                    Newtonsoft.Json.Linq.JArray FilterData = (Newtonsoft.Json.Linq.JArray)filter;

                    Type t = Type.GetType(Convert.ToString(FilterData[0]));

                    if (t != null)
                    {
                        System.Reflection.ConstructorInfo ci = t.GetConstructor(new Type[] { typeof(string) });
                        ChanArchiver.Filters.IFilter fil = (ChanArchiver.Filters.IFilter)ci.Invoke(new object[] { Convert.ToString(FilterData[1]) });
                        this.my_filters.Add(fil);
                    }
                }
            }
        }


        private void Log(LogEntry lo)
        {
            if (Program.verbose) { Program.PrintLog(lo); }
            this.mylogs.Add(lo);
        }

    }
}
