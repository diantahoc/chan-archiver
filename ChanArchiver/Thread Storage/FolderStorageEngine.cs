using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Jayrock;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace ChanArchiver.Thread_Storage
{
    /// <summary>
    /// ChanArchiver legacy storage engine
    /// </summary>
    [Obsolete("Use SQLite engine instead", false)]
    public class FolderStorageEngine
        : IStorageEngine
    {
        public ThreadStoreStats StoreStats { get; private set; }

        public PostFormatter[] GetThread(string board, string id)
        {
            string thread_dir_path = Path.Combine(Program.post_files_dir, board, id);

            DirectoryInfo info = new DirectoryInfo(thread_dir_path);

            if (!info.Exists) { return new PostFormatter[] { }; }

            // optimized thread path
            string opt_path = Path.Combine(thread_dir_path, id + "-opt.json");

            List<PostFormatter> thread_pf = new List<PostFormatter>();

            if (File.Exists(opt_path))
            {
                JsonObject thread_data = JsonConvert.Import<JsonObject>(File.ReadAllText(opt_path));

                string data;

                if (thread_data.Names.Cast<string>().Contains("op"))
                {
                    data = thread_data["op"].ToString();

                    if (!string.IsNullOrEmpty(data))
                    {
                        thread_pf.Add(load_post_data_str(data, true));
                        // remove the op post
                        thread_data.Remove("op");
                    }
                }

                // sort the replies by their id
                foreach (string key in thread_data.Names.Cast<object>().OrderBy(x => Convert.ToInt32(x)))
                {
                    data = thread_data[key].ToString();

                    if (!string.IsNullOrEmpty(data))
                    {
                        thread_pf.Add(load_post_data_str(data, false));
                    }
                }
            }
            else
            {
                string data;
                string op = Path.Combine(thread_dir_path, "op.json");
                if (File.Exists(op))
                {
                    data = File.ReadAllText(op);
                    if (!string.IsNullOrEmpty(data))
                    {
                        thread_pf.Add(load_post_data_str(data, true));
                    }
                }

                foreach (string file in
                    Directory.EnumerateFiles(thread_dir_path, "*.json", SearchOption.TopDirectoryOnly).OrderBy(x => Path.GetFileNameWithoutExtension(x)))
                {
                    if (!file.EndsWith("op.json"))
                    {
                        data = File.ReadAllText(file);

                        if (!string.IsNullOrWhiteSpace(data))
                        {
                            try
                            {
                                PostFormatter post = load_post_data_str(data, false);
                                thread_pf.Add(post);
                            }
                            catch (JsonException)
                            {
                                File.Delete(file);
                            }
                            catch (Exception)
                            { }

                        }
                    }
                }
            }
            return thread_pf.ToArray();
        }

        public void DeleteThread(string board, string id)
        {
            string thread_dir_path = Path.Combine(Program.post_files_dir, board, id);

            if (Directory.Exists(thread_dir_path))
            {
                Directory.Delete(thread_dir_path, true);
            }
        }

        public IEnumerable<string> GetIndexIDOnly(string board)
        {
            string board_folder = Path.Combine(Program.post_files_dir, board);

            if (Directory.Exists(board_folder))
            {
                foreach (string dir in Directory.EnumerateDirectories(board_folder))
                {
                    yield return Path.GetFileName(dir);
                }
            }
        }

        public IEnumerable<string> GetExistingBoards()
        {
            if (Directory.Exists(Program.post_files_dir))
            {
                foreach (string dir in Directory.EnumerateDirectories(Program.post_files_dir))
                {
                    yield return Path.GetFileName(dir);
                }
            }
            else
            {
                yield break;
            }
        }

        public PostFormatter[] GetIndex(string board, int start = -1, int count = -1)
        {
            string board_folder = Path.Combine(Program.post_files_dir, board);

            if (Directory.Exists(board_folder))
            {
                IEnumerable<string> folders = null;

                if (start == -1)
                {
                    folders = Directory.EnumerateDirectories(board_folder).OrderByDescending(x => Path.GetFileName(x));
                }
                else
                {
                    var folders_names_ordered = Directory.EnumerateDirectories(board_folder).OrderByDescending(x => Path.GetFileName(x));

                    if (count == -1)
                    {
                        // skip n elements till the end
                        folders = folders_names_ordered.Skip(start);
                    }
                    else
                    {
                        // skip n elements till we have count items
                        List<string> list = new List<string>();
                        int index = 0;
                        foreach (string path in folders_names_ordered)
                        {
                            if (list.Count == count)
                            {
                                break;
                            }
                            if (index >= start)
                            {
                                list.Add(path);
                            }
                            index++;
                        }
                        folders = list;
                    }
                }

                List<PostFormatter> threads_pf = new List<PostFormatter>(folders.Count());

                foreach (string thread_folder in folders)
                {
                    string op_file = Path.Combine(thread_folder, "op.json");

                    if (File.Exists(op_file))
                    {
                        threads_pf.Add(load_post_data_str(File.ReadAllText(op_file), true));
                        continue;
                    }

                    string dir_name = Path.GetFileName(thread_folder);
                    string optimized = Path.Combine(thread_folder, dir_name + "-opt.json");

                    if (File.Exists(optimized))
                    {
                        try
                        {
                            JsonObject t = JsonConvert.Import<JsonObject>(File.ReadAllText(optimized));
                            if (t.Names.Cast<string>().Contains("op"))
                            {
                                threads_pf.Add(load_post_data_str(t["op"].ToString(), true));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Cannot decode optimized thread file {0}-{1}: {2}", board, dir_name, ex.Message);
                        }
                    }
                }

                return threads_pf.ToArray();
            }
            else
            {
                return new PostFormatter[0];
            }
        }

        private int CountThreads(string board)
        {
            string board_folder = Path.Combine(Program.post_files_dir, board);

            if (Directory.Exists(board_folder))
            {
                return Directory.EnumerateDirectories(board_folder).Count();
            }
            else
            {
                return 0;
            }
        }

        private PostFormatter load_post_data_str(string data, bool isop)
        {
            JsonObject post_data = JsonConvert.Import<JsonObject>(data);

            string[] keys = post_data.Names.Cast<string>().ToArray();

            PostFormatter pf = new PostFormatter();

            foreach (JsonMember member in post_data)
            {
                switch (member.Name)
                {
                    case "RawComment":
                        pf.Comment = Convert.ToString(member.Value);
                        continue;
                    case "Email":
                        pf.Email = Convert.ToString(member.Value);
                        continue;
                    case "Name":
                        pf.Name = Convert.ToString(member.Value);
                        continue;
                    case "PosterID":
                        pf.PosterID = Convert.ToString(member.Value);
                        continue;
                    case "Subject":
                        pf.Subject = Convert.ToString(member.Value);
                        continue;
                    case "Trip":
                        pf.Trip = Convert.ToString(member.Value);
                        continue;
                    case "ID":
                        pf.PostID = Convert.ToInt32(member.Value);
                        continue;
                    case "Time":
                        try
                        {
                            pf.Time = DateTime.Parse(member.Value.ToString());
                        }
                        catch
                        {

                        }
                        continue;
                    case "FileHash":
                        {
                            FileFormatter f = new FileFormatter();
                            f.PostID = pf.PostID;
                            f.FileName = Convert.ToString(post_data["FileName"]);
                            f.Hash = Convert.ToString(post_data["FileHash"]);
                            f.ThumbName = Convert.ToString(post_data["ThumbTime"]);
                            f.Height = Convert.ToInt32(post_data["FileHeight"]);
                            f.Width = Convert.ToInt32(post_data["FileWidth"]);
                            f.Size = Convert.ToInt32(post_data["FileSize"]);
                            pf.MyFile = f;
                            continue;
                        }
                    case "Sticky":
                        pf.IsSticky = Convert.ToBoolean(member.Value);
                        continue;
                    case "Closed":
                        pf.IsLocked = Convert.ToBoolean(member.Value);
                        continue;
                }
            }
            pf.Type = isop ? PostFormatter.PostType.OP : PostFormatter.PostType.Reply;
            return pf;
        }

        public void UpdateThreadStoreStats()
        {
            ThreadStoreStats stats;

            if (StoreStats == null)
            {
                stats = new ThreadStoreStats();
                StoreStats = stats;
            }
            else
            {
                stats = StoreStats;
            }

            int totalCount = 0;

            foreach (string board in Program.ValidBoards.Keys)
            {
                int threadCount = CountThreads(board);
                stats[board] = threadCount;
                totalCount += threadCount;
            }

            stats.TotalArchivedThreadsCount = totalCount;
        }

        public string GetThreadNotes(string board, int tid)
        {
            string thread_dir_path = Path.Combine(Program.post_files_dir, board, tid.ToString());
            string notes_file_path = Path.Combine(thread_dir_path, "notes.txt");
            if (File.Exists(notes_file_path))
            {
                return File.ReadAllText(notes_file_path);
            }
            else { return string.Empty; }
        }

        public void setThreadNotes(string board, int tid, string notes)
        {
            if (Program.IsBoardLetterValid(board))
            {
                string thread_dir_path = Path.Combine(Program.post_files_dir, board, tid.ToString());

                Directory.CreateDirectory(thread_dir_path); // prevent directory not found exceptions

                string notes_file_path = Path.Combine(thread_dir_path, "notes.txt");

                File.WriteAllText(notes_file_path, notes);
            }
        }
    }
}
