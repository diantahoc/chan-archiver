using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AniWrap.DataTypes;

namespace ChanArchiver.Thread_Storage
{
    public class DummyStorageImplementation
        : IStorageEngine
    {
        public PostFormatter[] GetThread(string board, string id)
        {
            return new PostFormatter[0];
        }

        public void DeleteThread(string board, string id)
        {

        }

        public IEnumerable<string> GetIndexIDOnly(string board)
        {
            yield break;
        }

        public IEnumerable<string> GetExistingBoards()
        {
            yield break;
        }

        public PostFormatter[] GetIndex(string board, int start = -1, int count = -1)
        {
            return new PostFormatter[0];
        }

        public void UpdateThreadStoreStats()
        {

        }

        public ThreadStoreStats StoreStats { get { return new ThreadStoreStats(); } }

        public string GetThreadNotes(string board, int tid) { return string.Empty; }

        public void setThreadNotes(string board, int tid, string notes) { }

        public void savePost(string board, int tid, int postId, GenericPost post) { }

        public void OptimizeThread(string board, int tid) { }
    }
}
