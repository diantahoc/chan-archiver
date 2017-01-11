using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    public class FileIndexInfo
    {
        private Dictionary<string, Post> my_posts = new Dictionary<string, Post>();

        public string Hash { get; private set; }

        public Post[] GetRepostsData() { return my_posts.Values.ToArray(); }

        public int RepostCount
        {
            get
            {
                return my_posts.Count();
            }
        }

        public FileIndexInfo(string hash)
        {
            this.Hash = hash;
        }

        public struct Post
        {
            public string Board;
            public int ThreadID;
            public int PostID;
            public string FileName;
            //public string ToString()
            //{
            //    return string.Format("{0}-{1}-{2}", this.Board, this.ThreadID, this.PostID);
            //}
        }

        private string get_post_hash(string board, int threadid, int postid)
        {
            return board + "-" + threadid + "-" + postid;
        }

        public void MarkPost(string board, int threadid, int postid, string file_name)
        {
            string hash = get_post_hash(board, threadid, postid);
            lock (my_posts)
            {
                if (!my_posts.ContainsKey(hash))
                {
                    my_posts.Add(hash, new Post()
                    {
                        Board = board,
                        ThreadID = threadid,
                        PostID = postid,
                        FileName = file_name
                    });
                }
            }
        }

        public void RemovePost(int id)
        {
            var file = my_posts.Where(x => x.Value.PostID == id).ToArray();
            foreach (var f in file)
                my_posts.Remove(f.Key);
        }
    }
}
