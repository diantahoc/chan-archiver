﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using AniWrap.DataTypes;
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

        public ThreadWorker(BoardWatcher board, int id)
        {
            this.ID = id;
            this.Board = board;
            this.LastUpdated = DateTime.Now;

            this.BumpLimit = 300;
            this.ImageLimit = 151;
            this.UpdateInterval = board.ThreadWorkerInterval;

            worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
        }

        int old_replies_count = 0;

        private bool running = true;

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string thread_folder = Path.Combine(Program.post_files_dir, this.Board.Board, this.ID.ToString());

            Directory.CreateDirectory(thread_folder);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            while (running)
            {
                ThreadContainer tc = null;
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

                    tc = Program.aw.GetThreadData(this.Board.Board, this.ID);

                   
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

                    string op = Path.Combine(thread_folder, "op.json");

                    if (!File.Exists(op))
                    {
                        string post_data = get_post_string(tc.Instance);

                        if (tc.Instance.File != null) { Program.dump_files(tc.Instance.File); }

                        File.WriteAllText(op, post_data);
                    }

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
                            with_image++; Program.dump_files(tc.Replies[i].File);
                        }
                    }

                    sw.Stop();

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
                        //auto-sage mode, we must archive faster
                        if (this.Board.Speed == BoardWatcher.BoardSpeed.Fast)
                        {
                            this.UpdateInterval = 0.5; //each 30 sec
                        }
                        else if (this.Board.Speed == BoardWatcher.BoardSpeed.Normal)
                        {
                            this.UpdateInterval = 1; //each 60 sec
                        }
                    }

                    //System.Threading.Tasks.Task wait = new System.Threading.Tasks.Task((Action)delegate 
                    //    {
                    //        int seconds_to_wait = (int)this.UpdateInterval * 60;
                    //        for (int i = 0; i < seconds_to_wait; i++) 
                    //        {
                    //            if (running)
                    //            {
                    //                System.Threading.Thread.Sleep(1000); //1 sec
                    //            }
                    //            else 
                    //            {
                    //                return;
                    //            }
                    //        }
                    //    });

                    //wait.Start();
                    //wait.Wait();

                    System.Threading.Thread.Sleep(Convert.ToInt32(this.UpdateInterval * 60 * 1000));
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("404"))
                    {
                        Thread404(this);
                        return;
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


            log(new LogEntry()
            {
                Level = LogEntry.LogLevel.Success,
                Message = "Stopped thread worker successfully",
                Sender = "ThreadWorker",
                Title = string.Format("/{0}/ - {1}", this.Board.Board, this.ID)
            });

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

        private static string get_post_string(GenericPost gp)
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
                dic.Add("RawComment", gp.Comment);

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

            dic.Add("Time", gp.Time);

            if (gp.File != null)
            {
                dic.Add("FileHash", Program.base64tostring(gp.File.hash));
                dic.Add("FileName", gp.File.filename + "." + gp.File.ext);
                dic.Add("ThumbTime", gp.File.thumbnail_tim);
                dic.Add("FileHeight", gp.File.height);
                dic.Add("FileWidth", gp.File.width);
                dic.Add("FileSize", gp.File.size);

            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(dic, Newtonsoft.Json.Formatting.Indented);
        }

        public void Start()
        {
            if (!worker.IsBusy) { running = true; worker.RunWorkerAsync(); }
        }

        public delegate void Thread404Event(ThreadWorker instance);

        public event Thread404Event Thread404;

        public void Dispose()
        {
            this.worker.Dispose();
        }

    }
}
