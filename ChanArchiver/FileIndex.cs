using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Jayrock.Json.Conversion;
using Jayrock.Json;
using System.Threading;

namespace ChanArchiver
{
    public static class FileIndex
    {
        private static Dictionary<string, FileIndexInfo> file_index = new Dictionary<string, FileIndexInfo>();

        private static bool need_save = false;

        public static void Load()
        {
            Console.Write("Loading file index ...");
            if (File.Exists(FileIndexSaveFile))
            {
                load_index_from_save_file();
            }
            else
            {
                build_file_index_from_scratch();
                save_index();
            }

            Console.WriteLine(".. done");
        }

        public static void Save()
        {
            if (need_save)
            {
                save_index();
            }
        }

        public static void ReBuild()
        {
            build_file_index_from_scratch();
        }

        //public static void BuildIndex() { }

        private static readonly object l = new object();

        private static void build_file_index_from_scratch()
        {
            lock (l)
            {
                file_index.Clear();
                GC.Collect();
                try
                {
                    foreach (string board in ThreadStore.GetExistingBoards())
                    {
                        var threads = ThreadStore.GetIndexIDOnly(board);

                        foreach (string thread_id in threads)
                        {
                            if (thread_id == null) { continue; }

                            var thread_data = ThreadStore.GetThread(board, thread_id);

                            int tid = int.Parse(thread_id);

                            foreach (var post in thread_data)
                            {
                                if (post.MyFile != null)
                                {
                                    FileIndexInfo w;

                                    if (file_index.ContainsKey(post.MyFile.Hash))
                                    {
                                        w = file_index[post.MyFile.Hash];
                                    }
                                    else
                                    {
                                        w = new FileIndexInfo(post.MyFile.Hash);
                                        file_index.Add(post.MyFile.Hash, w);
                                    }

                                    w.MarkPost(board, tid, post.PostID, post.MyFile.FileName);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    file_index.Clear();
                    Console.WriteLine("Error occured while building file index:\n{0}\n{1}", ex.Message, ex.StackTrace);
                    Console.Beep();
                }
            }
        }

        private static string FileIndexSaveFile
        {
            get
            {
                return System.IO.Path.Combine(Program.program_dir, "file-index.json");
            }
        }

        const string FILE_HASH = "H";
        const string DATA = "D";
        const string BOARD = "B";
        const string FILE_NAME = "N";
        const string THREAD_ID = "T";
        const string POST_ID = "P";

        private static void save_index()
        {
            lock (l)
            {
                using (TextWriter tw = File.CreateText(FileIndexSaveFile))
                {
                    using (JsonTextWriter jw = new JsonTextWriter(tw))
                    {
                        jw.PrettyPrint = false;

                        jw.WriteStartArray();

                        foreach (var kvp in file_index)
                        {
                            jw.WriteStartObject();

                            jw.WriteMember(FILE_HASH);
                            jw.WriteString(kvp.Key);

                            jw.WriteMember(DATA);

                            jw.WriteStartArray();

                            foreach (var i in kvp.Value.GetRepostsData())
                            {
                                jw.WriteStartObject();

                                jw.WriteMember(BOARD);
                                jw.WriteString(i.Board);

                                jw.WriteMember(FILE_NAME);
                                jw.WriteString(i.FileName);

                                jw.WriteMember(THREAD_ID);
                                jw.WriteNumber(i.ThreadID);

                                jw.WriteMember(POST_ID);
                                jw.WriteNumber(i.PostID);

                                jw.WriteEndObject();
                            }

                            jw.WriteEndArray();

                            jw.WriteEndObject();
                        }

                        jw.WriteEndArray();
                    }
                }
            }
            need_save = false;
        }

        private static void load_index_from_save_file()
        {
            lock (l)
            {
                using (TextReader tr = File.OpenText(FileIndexSaveFile))
                {
                    using (JsonTextReader jtr = new JsonTextReader(tr))
                    {
                        JsonArray obj = JsonConvert.Import<JsonArray>(jtr);

                        foreach (JsonObject index_entry in obj)
                        {
                            string hash = index_entry[FILE_HASH].ToString();

                            JsonArray data = (JsonArray)index_entry[DATA];

                            if (data.Count > 0)
                            {
                                FileIndexInfo info = new FileIndexInfo(hash);

                                foreach (JsonObject file_data in data)
                                {
                                    try
                                    {
                                        info.MarkPost(
                                            board: file_data[BOARD].ToString(),
                                            file_name: file_data[FILE_NAME].ToString(),
                                            threadid: Convert.ToInt32(file_data[THREAD_ID]),
                                            postid: Convert.ToInt32(file_data[POST_ID]));
                                    }
                                    catch (Exception) { }
                                }

                                file_index.Add(hash, info);
                            }
                        }
                    }
                }
            }
            GC.Collect();
        }

        public static FileIndexInfo GetIndexState(string hash)
        {
            if (file_index.ContainsKey(hash))
                return file_index[hash];
            else
                return null;
        }

        public static int EntriesCount { get { return file_index.Count; } }

        public static bool IsHashValid(string hash)
        {
            return file_index.ContainsKey(hash);
        }

        private static object g = new object();

        public static void MarkPostAsync(string hash, AniWrap.DataTypes.PostFile p)
        {
            (new Thread(() =>
            {
                MarkPost(hash, p);
            })).Start();
        }

        public static void MarkPost(string hash, AniWrap.DataTypes.PostFile p)
        {
            lock (g)
            {
                FileIndexInfo w = null;
                if (file_index.ContainsKey(hash))
                {
                    w = file_index[hash];
                }
                else
                {
                    w = new FileIndexInfo(hash);
                    file_index.Add(hash, w);
                }

                w.MarkPost(p.board, p.owner.OwnerThread.ID, p.owner.ID, p.filename + "." + p.ext);
                need_save = true;
            }
        }

        public static void RemovePost(string hash, int postid)
        {
            lock (g)
            {
                if (file_index.ContainsKey(hash))
                {
                    var w = file_index[hash];

                    w.RemovePost(postid);

                    if (w.RepostCount == 0)
                    {
                        file_index.Remove(hash);
                    }
                }
                need_save = true;
            }
        }
    }
}
