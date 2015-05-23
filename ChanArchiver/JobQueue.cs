using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChanArchiver
{
    public class JobQueue
    {
        private Func<QueueItem, object> sorter = (x => { return x.FileInfo.PostFile.size; });

        private readonly object lockObject = new object();

        private readonly object lockObject1 = new object();

        private List<QueueItem> _queue;

        private Thread _worker;

        public JobQueue(string QueueName)
        {
            this._queue = new List<QueueItem>();

            ParameterizedThreadStart pts = new ParameterizedThreadStart(work);
            _worker = new Thread(pts);
            _worker.Start();
        }

        private int current_active = 0;

        public int MaxActive { get; set; }

        private void work(object s)
        {
            while (true)
            {
                lock (lockObject)
                {
                    if (current_active < _queue.Count)
                    {
                        Func<QueueItem, object> sorter_func = null;

                        lock (lockObject1) 
                        {
                            sorter_func = sorter;
                        }

                        QueueItem i = _queue.OrderBy(sorter_func).First(); _queue.Remove(i);

                        Task.Factory.StartNew(new Action(() =>
                        {
                            Interlocked.Increment(ref current_active);

                            try
                            {
                                i.Action();
                            }
                            catch
                            {

                            }
                            Interlocked.Decrement(ref current_active);
                        }));
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public void Enqueue(QueueItem item)
        {
            lock (lockObject)
            {
                _queue.Add(item);
            }
        }


        public FileQueueStateInfo GetNextExecutedFile()
        {
            lock (lockObject)
            {
                var f = _queue.OrderBy(sorter);
                if (f.Count() > 0)
                {
                    return f.First().FileInfo;
                }
            }
            return null;
        }

        public void SetSorterFunction(Func<QueueItem, object> func) 
        {
            lock (lockObject1)
            {
                this.sorter = func;
            }
        }
    }

    public class QueueItem
    {
        public FileQueueStateInfo FileInfo { get; set; }
        public Action Action { get; set; }

        private static Action empty = new Action(() => { });

        public QueueItem()
        {
            this.Action = empty;
        }
    }
}
