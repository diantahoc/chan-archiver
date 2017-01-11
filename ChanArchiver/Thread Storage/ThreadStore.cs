using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Jayrock;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using ChanArchiver.Thread_Storage;

namespace ChanArchiver
{
    /// <summary>
    /// Proxy class for the actual storage engine implementation
    /// </summary>
    public static class ThreadStore
    {
        private static IStorageEngine storageImpl;

        static ThreadStore()
        {
            storageImpl = new DummyStorageImplementation();
        }

        public static void SetUp(IStorageEngine engine)
        {
            if (engine == null)
            {
                throw new ArgumentNullException("engine");
            }
            storageImpl = engine;
        }
        /*
        [Obsolete("Use GetStorageEngine instead", true)]
        public static ThreadStoreStats StoreStats
        {
            get
            {
                return storageImpl.StoreStats;
            }
        }

        [Obsolete("Use GetStorageEngine instead", true)]
        public static PostFormatter[] GetThread(string board, string id)
        {
            return storageImpl.GetThread(board, id);
        }

        [Obsolete("Use GetStorageEngine instead", true)]
        public static void DeleteThread(string board, string id)
        {
            storageImpl.DeleteThread(board, id);
        }

        [Obsolete("Use GetStorageEngine instead", true)]
        public static IEnumerable<string> GetIndexIDOnly(string board)
        {
            return storageImpl.GetIndexIDOnly(board);
        }

        [Obsolete("Use GetStorageEngine instead", true)]
        public static IEnumerable<string> GetExistingBoards()
        {
            return storageImpl.GetExistingBoards();
        }

        [Obsolete("Use GetStorageEngine instead", true)]
        public static PostFormatter[] GetIndex(string board, int start = -1, int count = -1)
        {
            return storageImpl.GetIndex(board, start, count);
        }

        [Obsolete("Use GetStorageEngine instead", true)]
        public static void UpdateThreadStoreStats()
        {
            storageImpl.UpdateThreadStoreStats();
        }
        */
        public static IStorageEngine GetStorageEngine()
        {
            return storageImpl;
        }
    }
}
