using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.Thread_Storage
{
    public class ThreadStoreStats
    {
        public int TotalArchivedThreadsCount { get; set; }
        private Dictionary<string, int> _data;

        public ThreadStoreStats()
        {
            _data = new Dictionary<string, int>(Program.ValidBoards.Count);
        }

        public int this[string a]
        {
            get
            {
                if (this._data.ContainsKey(a))
                {
                    return _data[a];
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (this._data.ContainsKey(a))
                {
                    _data[a] = value;
                }
                else
                {
                    _data.Add(a, value);
                }
            }
        }
    }
}
