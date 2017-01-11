using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using AniWrap.DataTypes;
using System.Text.RegularExpressions;
using Jayrock.Json;
using Jayrock.Json.Conversion;
namespace ChanArchiver
{
    public class ThreadWorker : IDisposable
    {
        public int ID { get; private set; }
        public BoardWatcher Board { get; private set; }

        public int BumpLimit { get; set; }
        public int ImageLimit { get; set; }

        public DateTime LastUpdated { get; private set; }

        /// <summary>
        /// A boolean value to indicate if this thread worker has been automatically started by a board watcher.
        /// </summary>

        public bool AddedAutomatically { get; set; }

        private BackgroundWorker worker;

        public bool IsActive { get { return this.worker.IsBusy || running; } }

        private List<LogEntry> mylogs = new List<LogEntry>();

        public LogEntry[] Logs { get { return this.mylogs.ToArray(); } }

        public double UpdateInterval { get; set; }

        public bool ThumbOnly { get; set; }

        public bool AutoSage { get; private set; }
        public bool ImageLimitReached { get; private set; }

        public bool IsStatic { get; private set; }

        public ThreadWorker(BoardWatcher board, int id)
        {
            this.ID = id;
            this.Board = board;
            this.LastUpdated = DateTime.Now;

            this.BumpLimit = 300;
            this.ImageLimit = 151;
            this.UpdateInterval = board.ThreadWorkerInterval;

            this.ThumbOnly = Settings.ThumbnailOnly;

            worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

            worker.DoWork += new DoWorkEventHandler(worker_DoWork);

            board.FiltersUpdated += this.board_FiltersUpdated;
        }

        /// <summary>
        /// This constructor is only used to save dead (from archive) threads
        /// </summary>
        /// <param name="board"></param>
        /// <param name="tc"></param>
        public ThreadWorker(BoardWatcher board, ThreadContainer tc, bool thumbOnly)
        {
            this.ID = tc.Instance.ID;
            this.Board = board;
            this.LastUpdated = tc.Instance.Time;
            this.AddedAutomatically = false;
            this.IsStatic = true;
            this.worker = new BackgroundWorker();
            this.ThumbOnly = thumbOnly;
            this.ThreadTitle = tc.Title;

            save_thread_container(tc);
        }

        private void board_FiltersUpdated()
        {
            log(new LogEntry()
            {
                Level = LogEntry.LogLevel.Info,
                Message = "Board filters have been changed, re-checking if I can run...",
                Sender = "ThreadWorker",
                Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
            });

            if (this.Board.Mode == BoardWatcher.BoardMode.Whitelist || this.Board.Mode == BoardWatcher.BoardMode.Blacklist)
            {
                if (this.AddedAutomatically && this.IsActive)
                {
                    this.Start();
                }
            }
        }

        int old_replies_count = 0;

        private bool running = false;

        public string ThreadTitle { get; private set; }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string thread_folder = Path.Combine(Program.post_files_dir, this.Board.Board, this.ID.ToString());

            Directory.CreateDirectory(thread_folder);

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            while (running)
            {
                sw.Reset();
                try
                {
                    sw.Start();

                    log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Info,
                        Message = "Updating thread...",
                        Sender = "ThreadWorker",
                        Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
                    });

                    var tc = Program.aw.GetThreadData(this.Board.Board, this.ID);

                    this.ThreadTitle = tc.Title;

                    if (!can_i_run(tc.Instance))
                    {
                        log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Info,
                            Message = "ThreadWorker stopped because of a filter",
                            Sender = "ThreadWorker",
                            Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
                        });
                        running = false;
                        ThreadStore.GetStorageEngine().DeleteThread(this.Board.Board, this.ID.ToString());
                        break;
                    }


                    if (this.AddedAutomatically && this.Board.Mode == BoardWatcher.BoardMode.Harvester)
                    {
                        if (tc.Instance.File != null)
                        {
                            if (this.Board.IsFileAllowed(tc.Instance.File.ext))
                            {
                                savePost(tc.Instance);

                                Program.dump_files(tc.Instance.File, this.ThumbOnly);
                            }
                        }
                    }
                    else
                    {
                        savePost(tc.Instance);

                        if (tc.Instance.File != null) { Program.dump_files(tc.Instance.File, this.ThumbOnly); }
                    }


                    int count = tc.Replies.Count();

                    int with_image = 0;

                    for (int i = 0; i < count; i++)
                    {
                        if (this.AddedAutomatically && this.Board.Mode == BoardWatcher.BoardMode.None) { continue; }

                        if (this.AddedAutomatically && this.Board.Mode == BoardWatcher.BoardMode.Harvester)
                        {
                            if (tc.Replies[i].File != null)
                            {
                                if (!this.Board.IsFileAllowed(tc.Replies[i].File.ext))
                                {
                                    continue;
                                }
                            }
                            else { continue; }
                        }

                        GenericPost replyPost = tc.Replies[i];
                        savePost(replyPost);

                        if (tc.Replies[i].File != null)
                        {
                            ++with_image;
                            Program.dump_files(tc.Replies[i].File, this.ThumbOnly);
                        }
                    }

                    sw.Stop();

                    this.ImageLimitReached = with_image >= this.ImageLimit;

                    int new_rc = count - old_replies_count;

                    log(new LogEntry()
                    {
                        Level = LogEntry.LogLevel.Success,
                        Message = string.Format("Updated in {0} seconds {1}", sw.Elapsed.Seconds, new_rc > 0 ? ", + " + new_rc.ToString() + " new replies" : ""),
                        Sender = "ThreadWorker",
                        Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
                    });

                    old_replies_count = count;
                    this.LastUpdated = DateTime.Now;

                    if (count >= this.BumpLimit)
                    {
                        this.AutoSage = true;
                        //auto-sage mode, we must archive faster
                        if (this.Board.Speed == BoardWatcher.BoardSpeed.Fast)
                        {
                            this.UpdateInterval = 0.16; //each 10 sec
                        }
                        else if (this.Board.Speed == BoardWatcher.BoardSpeed.Normal)
                        {
                            this.UpdateInterval = 1; //each 60 sec
                        }
                    }


                    if (tc.Instance.IsSticky) { this.UpdateInterval = 5; }

                    if (tc.Instance.IsArchived)
                    {
                        log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Info,
                            Message = string.Format("Thread entered archived state."),
                            Sender = "ThreadWorker",
                            Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
                        });


                        ThreadStore.GetStorageEngine().OptimizeThread(this.Board.Board, this.ID);

                        this.Stop();

                        if (Settings.RemoveThreadsWhenTheyEnterArchivedState && !this.AddedAutomatically)
                        {
                            Thread404(this);
                        }
                        else
                        {
                            goto stop;
                        }
                    }

                    if (this.Board.Mode == BoardWatcher.BoardMode.Harvester) { this.UpdateInterval = 2; }

                    System.Threading.Thread.Sleep(Convert.ToInt32(this.UpdateInterval * 60 * 1000));
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Info,
                            Message = string.Format("Optimizing thread data..."),
                            Sender = "ThreadWorker",
                            Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
                        });

                        if (!(this.AddedAutomatically && this.Board.Mode == BoardWatcher.BoardMode.Harvester))
                        {
                            ThreadStore.GetStorageEngine().OptimizeThread(this.Board.Board, this.ID);
                        }

                        this.Stop();
                        Thread404(this);
                        goto stop;
                    }
                    else
                    {
                        log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Fail,
                            Message = string.Format("An error occured '{0}' @ '{1}', retrying", ex.Message, ex.StackTrace),
                            Sender = "ThreadWorker",
                            Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
                        });
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

        stop:

            log(new LogEntry()
            {
                Level = LogEntry.LogLevel.Success,
                Message = "Stopped thread worker successfully",
                Sender = "ThreadWorker",
                Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
            });
        }

        private void savePost(GenericPost gp)
        {
            ThreadStore.GetStorageEngine().savePost(this.Board.Board, this.ID, gp.ID, gp);
        }

        private void save_thread_container(ThreadContainer tc)
        {
            savePost(tc.Instance);

            if (tc.Instance.File != null) { Program.dump_files(tc.Instance.File, this.ThumbOnly); }

            int count = tc.Replies.Count();

            int with_image = 0;

            for (int i = 0; i < count; i++)
            {

                GenericPost reply = tc.Replies[i];

                savePost(reply);

                if (reply.File != null)
                {
                    ++with_image;
                    Program.dump_files(tc.Replies[i].File, this.ThumbOnly);
                }
            }

            log(new LogEntry()
            {
                Level = LogEntry.LogLevel.Success,
                Message = string.Format("Static thread {0} was saved successfully.", this.ID),
                Sender = "ThreadWorker",
                Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
            });

            log(new LogEntry() { Level = LogEntry.LogLevel.Info, Message = "Optimizing thread data." });

            ThreadStore.GetStorageEngine().OptimizeThread(this.Board.Board, this.ID);

            log(new LogEntry() { Level = LogEntry.LogLevel.Success, Message = "Optimisation done." });
        }

        private bool can_i_run(GenericPost po)
        {
            if (this.AddedAutomatically)
            {
                // If the container board is in whitelist mode, only start the worker when there is matching filters 
                // If the container board is in blacklist mode, only start the worker when there is NO matching filters 

                if (this.Board.Mode == BoardWatcher.BoardMode.Whitelist)
                {
                    return this.Board.MatchFilters(po);
                }
                else if (this.Board.Mode == BoardWatcher.BoardMode.Blacklist)
                {
                    return !this.Board.MatchFilters(po);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        public void Stop()
        {
            if (this.IsStatic) { return; }

            running = false;
            worker.CancelAsync();

            log(new LogEntry()
           {
               Level = LogEntry.LogLevel.Info,
               Message = "Stopping thread worker...",
               Sender = "ThreadWorker",
               Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
           });
        }

        private void log(LogEntry lo)
        {
            this.mylogs.Add(lo);
            if (Program.verbose)
            {
                Program.PrintLog(lo);
            }
        }

        public void Start()
        {
            if (this.IsStatic) { return; }

            if (!worker.IsBusy)
            {
                running = true;
                worker.RunWorkerAsync();
            }
        }

        public delegate void Thread404Event(ThreadWorker instance);

        public event Thread404Event Thread404;

        public void Dispose()
        {
            if (this.IsStatic)
            {
                this.worker.Dispose();
                return;
            }
            this.Board.FiltersUpdated -= this.board_FiltersUpdated;
            this.worker.Dispose();
        }

    }
}
