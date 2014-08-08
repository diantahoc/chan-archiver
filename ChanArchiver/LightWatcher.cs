using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    /// <summary>
    /// Lightweight board watcher
    /// </summary>
    public class LightWatcher
    {
        public string Board { get; private set; }

        public LightWatcher(string board)
        {
            this.Board = board;
        }

        private bool running = true;

        private int[] old_ids = null;

        public void Start()
        {
            System.Threading.Tasks.Task.Factory.StartNew((Action)delegate
            {
                while (running)
                {
                    try
                    {
                        int[] new_data = Program.aw.GetBoardThreadsID(this.Board).Keys.ToArray();

                        if (DataRefreshed != null)
                        {
                            if (old_ids == null)
                            {
                                old_ids = new_data;
                                DataRefreshed(new_data);
                            }
                            else
                            {
                                List<int> new_ids = new List<int>();
                                foreach (int id in new_data)
                                {
                                    if (Array.IndexOf(old_ids, id) != -1)
                                    {
                                        new_ids.Add(id);
                                    }
                                }
                                old_ids = new_data;
                                DataRefreshed(new_ids.ToArray());
                            }
                        }

                        System.Threading.Thread.Sleep(1000 * 2 * 60);
                    }
                    catch (Exception)
                    {

                        System.Threading.Thread.Sleep(1000 * 10); // HACK

                        //throw;
                    }
                }
            });
        }

        public delegate void DataRefreshedEvent(int[] list);
        public event DataRefreshedEvent DataRefreshed;

        public void Stop()
        {
            running = false;
        }

    }
}
