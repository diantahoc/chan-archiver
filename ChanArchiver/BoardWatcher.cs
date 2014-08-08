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

        private LightWatcher lw_w;
        private List<LogEntry> mylogs = new List<LogEntry>();

        private bool board_404 = false;

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

        //List<int> rejected_threads_because_of_filters = new List<int>();

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
                Task.Factory.StartNew(load_board_sleep_times);

                LoadFilters();
                LoadManuallyAddedThreads();
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

        private void load_board_sleep_times()
        {
            //speed up
            if (this.Board == "b" || this.Board == "v") 
            {
                this.Speed = BoardSpeed.Fast;
                return;
            }

            try
            {
                var e = Program.aw.GetBoardThreadsID(this.Board);

                if (e.Count() == 0) { return; }

                DateTime n = DateTime.UtcNow;

                int[] list = new int[e.Count];

                for (int i = 0; i < e.Count; i++)
                {
                    list[i] = Convert.ToInt32((n - e.ElementAt(i).Value).TotalSeconds);
                }

                int mode = compute_mode(list);

                if (mode < 60)
                {
                    this.Speed = BoardSpeed.Fast;
                }
                else if (mode >= 60 && mode <= 650)
                {
                    this.Speed = BoardSpeed.Normal;
                }
                else
                {
                    this.Speed = BoardSpeed.Slow;
                }

                this.Log(new LogEntry() { Level = LogEntry.LogLevel.Info, Message = "Board speed detected: " + this.Speed.ToString(), Sender = "BoardWatcher", Title = this.Board });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("404"))
                {
                    //board does not exist!
                    board_404 = true;
                    return;
                }
                this.Speed = BoardSpeed.Normal;
            }
        }

        //compute statistic mode value
        private int compute_mode(int[] ns)
        {
            Dictionary<int, int> a = new Dictionary<int, int>(ns.Length);

            foreach (int n in ns)
            {
                if (a.ContainsKey(n))
                {
                    a[n]++;
                }
                else
                {
                    a.Add(n, 1);
                }
            }

            return (a.OrderByDescending(c => c.Value).First().Key);
        }

        public void StartMonitoring(BoardMode mode)
        {
            if (board_404)
            {
                Log(new LogEntry()
                {
                    Level = LogEntry.LogLevel.Fail,
                    Message = "This board does not exist",
                    Sender = "BoardWatcher",
                    Title = string.Format("/{0}/", this.Board)
                });

                return;
            }

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

                    start_rss_watcher();

                    // log("Board watcher started");
                });
            }
        }

        public void AddThreadId(int id, bool thumbOnly)
        {
            ThreadWorker t = null;

            if (this.watched_threads.ContainsKey(id))
            {
                t = this.watched_threads[id];
            }
            else
            {
                t = new ThreadWorker(this, id);
                this.watched_threads.Add(id, t);
                t.Thread404 += this.handle_thread_404;
            }

            t.AddedAutomatically = false; // THIS IS CRITICAL, OTHERWISE IT PREVENT MANUALLY ADDED THREADS FROM STARTING IN MONITOR MODE
            t.ThumbOnly = thumbOnly;
            t.Start();

            SaveManuallyAddedThreads();
        }

        public void LoadManuallyAddedThreads()
        {
            if (System.IO.File.Exists(this.ManuallyAddedThreadsSaveFilePath))
            {
                string data = System.IO.File.ReadAllText(this.ManuallyAddedThreadsSaveFilePath);

                object des = null;

                try
                {
                    des = Newtonsoft.Json.JsonConvert.DeserializeObject(data);
                }
                catch (Exception) { return; }

                Type des_type = (des == null ? null : des.GetType());

                if (des_type == typeof(Newtonsoft.Json.Linq.JArray))
                {
                    // legacy save file, des is a was List<int> which is equivalent to JArray

                    Newtonsoft.Json.Linq.JArray w = des as Newtonsoft.Json.Linq.JArray;

                    foreach (Newtonsoft.Json.Linq.JToken id in w)
                    {
                        try
                        {
                            if (id.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
                            {
                                AddThreadId(Convert.ToInt32(id), Settings.ThumbnailOnly);
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                else if (des_type == typeof(Newtonsoft.Json.Linq.JObject))
                {
                    //new save file, which was a Dictionary<int, bool>
                    Newtonsoft.Json.Linq.JObject w = des as Newtonsoft.Json.Linq.JObject;

                    foreach (KeyValuePair<string, Newtonsoft.Json.Linq.JToken> thread in w)
                    {
                        try
                        {
                            AddThreadId(Convert.ToInt32(thread.Key), Convert.ToBoolean(thread.Value));
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }
        }

        public void SaveManuallyAddedThreads()
        {
            Dictionary<int, bool> dic = new Dictionary<int, bool>();

            for (int i = 0; i < watched_threads.Count(); i++)
            {
                try
                {
                    ThreadWorker tw = watched_threads.ElementAt(i).Value;
                    if (!tw.AddedAutomatically)
                    {
                        dic.Add(tw.ID, tw.ThumbOnly);
                    }
                }
                catch (Exception)
                {
                    if (i > watched_threads.Count() - 1) { break; }
                }
            }
            System.IO.File.WriteAllText(this.ManuallyAddedThreadsSaveFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(dic));
        }

        public int ActiveThreadWorkers
        {
            get
            {
                int act = 0;

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

                    int thread_count = 0;
                    int started_count = 0;

                    foreach (CatalogItem[] page in catalog)
                    {
                        foreach (CatalogItem thread in page)
                        {
                            thread_count++;

                            if (Mode == BoardMode.None)
                            {
                                //board watcher have been stopped, return
                                return;
                            }

                            ThreadWorker tw = null;

                            if (!watched_threads.ContainsKey(thread.ID))
                            {
                                tw = new ThreadWorker(this, thread.ID)
                                {
                                    ImageLimit = thread.ImageLimit,
                                    BumpLimit = thread.BumpLimit,
                                    AddedAutomatically = true
                                };

                                tw.Thread404 += this.handle_thread_404;

                                watched_threads.Add(thread.ID, tw);
                            }
                            else
                            {
                                tw = watched_threads[thread.ID];
                                if (!tw.AddedAutomatically)
                                {
                                    continue;
                                }
                            }

                            if (this.Mode == BoardMode.Monitor)
                            {
                                if (!this.MatchFilters(thread))
                                {
                                    //in monitor mode, don't auto-start unacceptable threads
                                    this.Log(new LogEntry()
                                    {
                                        Level = LogEntry.LogLevel.Info,
                                        Message = "Thread " + thread.ID.ToString() + " has no matching filter, skipping it.",
                                        Sender = "BoardWatcher",
                                        Title = ""
                                    });
                                }
                                else 
                                {
                                    started_count++;

                                    Log(new LogEntry()
                                    {
                                        Level = LogEntry.LogLevel.Info,
                                        Message = string.Format("Thread {0} has matching filters, starting it.", thread.ID),
                                        Sender = "BoardWatcher",
                                        Title = string.Format("/{0}/", this.Board)
                                    });

                                    tw.Start();
                                }
                            }
                            else if (this.Mode == BoardMode.FullBoard)
                            {
                                if (Program.verbose)
                                {
                                    Log(new LogEntry()
                                    {
                                        Level = LogEntry.LogLevel.Info,
                                        Message = string.Format("Starting thread worker for thread # {0}", thread.ID),
                                        Sender = "BoardWatcher",
                                        Title = string.Format("/{0}/", this.Board)
                                    });
                                }

                                started_count++;

                                Task.Factory.StartNew((Action)delegate
                                {
                                    //Allow 5 sec delay between thread startup
                                    System.Threading.Thread.Sleep(5000);
                                    tw.Start();
                                });
                            }

                        }
                    }

                    Log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Success,
                        Message = string.Format("Finished loading {0} thread, {1} started.", thread_count, started_count),
                        Sender = "BoardWatcher",
                        Title = string.Format("/{0}/", this.Board)
                    });

                    //breaks the while
                    break;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        //board does not exist!
                        board_404 = true;
                        return;
                    }
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
        }

        public void StopMonitoring()
        {
            this.Mode = BoardMode.None;

            if (this.lw_w != null)
            {
                this.lw_w.Stop();
            }

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

            this.mylogs.Clear();

            Log(new LogEntry()
            {
                Level = LogEntry.LogLevel.Success,
                Message = "Monitoring has been stopped",
                Sender = "BoardWatcher",
                Title = string.Format("/{0}/", this.Board)
            });
        }

        //private void catalog_loaded_callback()
        //{
        //    foreach (KeyValuePair<int, ThreadWorker> w in watched_threads)
        //    {
        //        if (w.Value.AddedAutomatically)
        //        {
        //            if (this.Mode == BoardMode.None || this.Mode == BoardMode.Monitor)
        //            {
        //                //board watcher have been stopped, exit
        //                return;
        //            }

        //            w.Value.Thread404 += this.handle_thread_404;

        //            w.Value.Start();

        //            if (Program.verbose)
        //            {
        //                Log(new LogEntry()
        //                {
        //                    Level = LogEntry.LogLevel.Info,
        //                    Message = "Starting thread worker '" + w.Value.ID + "'",
        //                    Sender = "BoardWatcher",
        //                    Title = string.Format("/{0}/", this.Board)
        //                });
        //            }

        //            System.Threading.Thread.Sleep(500); //Allow 0.5 seconds between thread worker startup
        //        }
        //    }
        //    start_rss_watcher();
        //}

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
            lw_w = new LightWatcher(this.Board);
            lw_w.DataRefreshed += this.handle_rss_watcher_newdata;
            lw_w.Start();

            Log(new LogEntry()
            {
                Level = LogEntry.LogLevel.Success,
                Message = "Lightweight board watcher has started",
                Sender = "LightWatcher",
                Title = string.Format("/{0}/", this.Board)
            });
        }

        private void handle_rss_watcher_newdata(int[] data)
        {
            foreach (int id in data)
            {
                if (this.Mode == BoardMode.None) { return; }

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
                            Sender = "LightWatcher",
                            Title = string.Format("/{0}/", this.Board)
                        });
                    }
                }
            }
        }

        public delegate void FiltersUpdatedEvent();

        public event FiltersUpdatedEvent FiltersUpdated;

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

                if (FiltersUpdated != null)
                {
                    FiltersUpdated();
                }
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
                catch (Exception e)
                {
                    continue;
                }
            }

            return false;
        }

        ///// <summary>
        ///// Runs when filters are changed (added or removed)
        ///// </summary>
        //private void ReCheckAllFailedThreads()
        //{
        //    if (this.Mode == BoardMode.Monitor)
        //    {
        //        for (int index = 0; index < rejected_threads_because_of_filters.Count; index++)
        //        {
        //            try
        //            {
        //                int tid = rejected_threads_because_of_filters[index];
        //                if (!this.watched_threads.ContainsKey(tid))
        //                {
        //                    ThreadWorker tw = new ThreadWorker(this, tid);
        //                    this.watched_threads.Add(tid, tw);
        //                    tw.Thread404 += this.handle_thread_404;
        //                    tw.AddedAutomatically = true;
        //                    tw.Start();
        //                }
        //                else
        //                {
        //                    ThreadWorker tw = watched_threads[tid];
        //                    tw.Start();
        //                }
        //            }
        //            catch (Exception)
        //            {
        //                if (index >= rejected_threads_because_of_filters.Count) { return; }
        //            }
        //        }
        //    }
        //}

        //public void MarkThreadAsFilterTestFailed(int id)
        //{
        //    if (this.Mode == BoardMode.Monitor)
        //    {
        //        if (!this.rejected_threads_because_of_filters.Contains(id)) { this.rejected_threads_because_of_filters.Add(id); }
        //    }
        //}

        public void SaveFilters()
        {
            List<string[]> s = new List<string[]>();

            ChanArchiver.Filters.IFilter[] filters = my_filters.ToArray();

            foreach (ChanArchiver.Filters.IFilter filter in filters)
            {
                s.Add(new string[] { filter.GetType().FullName, filter.FilterText, filter.Notes });
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

        private string ManuallyAddedThreadsSaveFilePath
        {
            get
            {
                return System.IO.Path.Combine(Program.board_settings_dir, string.Format("manuallyaddedthreads-{0}.json", this.Board));
            }
        }

        public void LoadFilters()
        {
            if (System.IO.File.Exists(this.FilterSaveFilePath))
            {
                List<object> s = Newtonsoft.Json.JsonConvert.DeserializeObject<List<object>>(System.IO.File.ReadAllText(this.FilterSaveFilePath));

                foreach (object filter in s)
                {
                    Newtonsoft.Json.Linq.JArray FilterData = (Newtonsoft.Json.Linq.JArray)filter;

                    Type t = Type.GetType(Convert.ToString(FilterData[0]));

                    if (t != null)
                    {
                        System.Reflection.ConstructorInfo ci = t.GetConstructor(new Type[] { typeof(string) });
                        ChanArchiver.Filters.IFilter fil = (ChanArchiver.Filters.IFilter)ci.Invoke(new object[] { Convert.ToString(FilterData[1]) });

                        if (FilterData.Count > 2)
                        {
                            fil.Notes = Convert.ToString(FilterData[2]);
                        }

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
