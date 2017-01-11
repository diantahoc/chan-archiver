using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AniWrap.DataTypes;

namespace ChanArchiver.Thread_Storage
{
    public interface IStorageEngine
    {
        PostFormatter[] GetThread(string board, string id);

        void DeleteThread(string board, string id);

        IEnumerable<string> GetIndexIDOnly(string board);

        IEnumerable<string> GetExistingBoards();

        PostFormatter[] GetIndex(string board, int start = -1, int count = -1);

        void UpdateThreadStoreStats();

        ThreadStoreStats StoreStats { get; }

        string GetThreadNotes(string board, int tid);

        void setThreadNotes(string board, int tid, string notes);

        void savePost(string board, int tid, int postId, GenericPost post);

        void OptimizeThread(string board, int tid);
    }
}
