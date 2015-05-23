﻿using System;
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

            if (this.AddedAutomatically && this.IsActive && this.Board.Mode == BoardWatcher.BoardMode.Monitor)
            {
                this.Start();
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

                    if (string.IsNullOrEmpty(this.ThreadTitle)) { this.ThreadTitle = tc.Title; }

                    if (!can_i_run(tc.Instance))
                    {
                        log(new LogEntry()
                        {
                            Level = LogEntry.LogLevel.Info,
                            Message = "ThreadWorker stopped because there is no matching filter",
                            Sender = "ThreadWorker",
                            Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
                        });
                        running = false;
                        Directory.Delete(thread_folder);
                        break;
                    }


                    if (this.AddedAutomatically && this.Board.Mode == BoardWatcher.BoardMode.Harvester)
                    {
                        if (tc.Instance.File != null)
                        {
                            if (this.Board.IsFileAllowed(tc.Instance.File.ext))
                            {
                                string op = Path.Combine(thread_folder, "op.json");

                                if (!File.Exists(op))
                                {
                                    string post_data = get_post_string(tc.Instance);
                                    File.WriteAllText(op, post_data);
                                }

                                Program.dump_files(tc.Instance.File, this.ThumbOnly);
                            }
                        }
                    }
                    else
                    {
                        string op = Path.Combine(thread_folder, "op.json");

                        if (!File.Exists(op))
                        {
                            string post_data = get_post_string(tc.Instance);
                            File.WriteAllText(op, post_data);
                        }

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

                        string item_path = Path.Combine(thread_folder, tc.Replies[i].ID.ToString() + ".json");

                        if (!File.Exists(item_path))
                        {
                            string post_data = get_post_string(tc.Replies[i]);
                            File.WriteAllText(item_path, post_data);
                        }

                        if (tc.Replies[i].File != null)
                        {
                            with_image++;
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


                        optimize_thread_file(thread_folder);

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
                            optimize_thread_file(thread_folder);
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

        private void save_thread_container(ThreadContainer tc)
        {
            string thread_folder = Path.Combine(Program.post_files_dir, this.Board.Board, this.ID.ToString());

            Directory.CreateDirectory(thread_folder);

            string op = Path.Combine(thread_folder, "op.json");

            if (!File.Exists(op))
            {
                string post_data = get_post_string(tc.Instance);
                File.WriteAllText(op, post_data);
            }

            if (tc.Instance.File != null) { Program.dump_files(tc.Instance.File, this.ThumbOnly); }

            int count = tc.Replies.Count();

            int with_image = 0;

            for (int i = 0; i < count; i++)
            {
                string item_path = Path.Combine(thread_folder, tc.Replies[i].ID.ToString() + ".json");

                if (!File.Exists(item_path))
                {
                    string post_data = get_post_string(tc.Replies[i]);
                    File.WriteAllText(item_path, post_data);
                }

                if (tc.Replies[i].File != null)
                {
                    with_image++;
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
            
            optimize_thread_file(thread_folder);

            log(new LogEntry() { Level = LogEntry.LogLevel.Success, Message = "Optimisation done." });
        }

        public static void optimize_thread_file(string thread_folder)
        {
            if (Directory.Exists(thread_folder))
            {
                DirectoryInfo t = new DirectoryInfo(thread_folder);

                if (File.Exists(Path.Combine(t.FullName, t.Name + "-opt.json"))) { return; }

                FileInfo[] files = t.GetFiles("*.json");

                Dictionary<string, string> il = new Dictionary<string, string>();

                foreach (FileInfo fi in files)
                {
                    il.Add(fi.Name.Split('.')[0], File.ReadAllText(fi.FullName));

                    File.Delete(fi.FullName);
                }

                string data = JsonConvert.ExportToString(il);
                File.WriteAllText(Path.Combine(thread_folder, t.Name + "-opt.json"), data);
            }
        }

        private bool can_i_run(AniWrap.DataTypes.GenericPost po)
        {
            if (this.AddedAutomatically)
            {
                //if the container board is in monitor mode, we need to see
                //if this thread has a matching filter
                //otherwise we should not start it.
                if (this.Board.Mode == BoardWatcher.BoardMode.Monitor)
                {
                    return this.Board.MatchFilters(po);
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

        private string get_post_string(GenericPost gp)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();

            if (gp.GetType() == typeof(AniWrap.DataTypes.Thread))
            {
                AniWrap.DataTypes.Thread t = (AniWrap.DataTypes.Thread)gp;
                dic.Add("Closed", t.IsClosed);
                dic.Add("Sticky", t.IsSticky);
            }

            dic.Add("Board", gp.Board);

            dic.Add("ID", gp.ID);

            dic.Add("Name", gp.Name);

            if (gp.Capcode != GenericPost.CapcodeEnum.None)
            {
                dic.Add("Capcode", gp.Capcode);
            }

            if (!string.IsNullOrEmpty(gp.Comment))
            {

                dic.Add("RawComment", Wordfilter.Process(gp.Comment));
                // dic.Add("FormattedComment", gp.CommentText);
            }

            /*// Flag stuffs*/

            if (!string.IsNullOrEmpty(gp.country_flag))
            {
                dic.Add("CountryFlag", gp.country_flag);
            }

            if (!string.IsNullOrEmpty(gp.country_name))
            {
                dic.Add("CountryName", gp.country_name);
            }

            /* Flag stuffs //*/

            if (!string.IsNullOrEmpty(gp.Email))
            {
                dic.Add("Email", gp.Email);
            }

            if (!string.IsNullOrEmpty(gp.Trip))
            {
                dic.Add("Trip", gp.Trip);
            }

            if (!string.IsNullOrEmpty(gp.Subject))
            {
                dic.Add("Subject", gp.Subject);
            }

            if (!string.IsNullOrEmpty(gp.PosterID))
            {
                dic.Add("PosterID", gp.PosterID);
            }

            dic.Add("Time", gp.Time.ToString());

            if (gp.File != null)
            {
                dic.Add("FileHash", Program.base64tostring(gp.File.hash));
                dic.Add("FileName", Wordfilter.Process(gp.File.filename) + "." + gp.File.ext);
                dic.Add("ThumbTime", gp.File.thumbnail_tim);
                dic.Add("FileHeight", gp.File.height);
                dic.Add("FileWidth", gp.File.width);
                dic.Add("FileSize", gp.File.size);
            }

            return JsonConvert.ExportToString(dic);
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
