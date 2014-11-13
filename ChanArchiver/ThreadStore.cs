﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ChanArchiver
{
    public static class ThreadStore
    {
        public static PostFormatter[] GetThread(string board, string id)
        {
            string thread_dir_path = Path.Combine(Program.post_files_dir, board, id);

            DirectoryInfo info = new DirectoryInfo(thread_dir_path);

            if (!info.Exists) { return new PostFormatter[] { }; }

            // optimized thread path
            string opt_path = Path.Combine(thread_dir_path, id + "-opt.json");

            List<PostFormatter> thread_pf = new List<PostFormatter>();

            if (File.Exists(opt_path))
            {
                var thread_data = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(opt_path));

                thread_pf.Add(load_post_data_str(thread_data["op"].ToString(), true));
                
                // remove the op post
                thread_data.Remove("op");
                
                // sort the replies by their id
                foreach (string key in thread_data.Keys.OrderBy(x => Convert.ToInt32(x)))
                {
                    thread_pf.Add(load_post_data_str(thread_data[key].ToString(), false));
                }
            }
            else
            {
                string op = Path.Combine(thread_dir_path, "op.json");
                if (File.Exists(op))
                {
                    thread_pf.Add(load_post_data_str(File.ReadAllText(op), true));
                }

                foreach (var file in info.GetFiles("*.json", SearchOption.TopDirectoryOnly).OrderBy(x => x.Name))
                {
                    if (file.Name != "op.json") 
                    {
                        thread_pf.Add(load_post_data_str(File.ReadAllText(file.FullName), false));
                    }
                }

            }
            return thread_pf.ToArray();
        }

        public static void DeleteThread(string board, string id)
        {
            string thread_dir_path = Path.Combine(Program.post_files_dir, board, id);

            if (Directory.Exists(thread_dir_path))
            {
                Directory.Delete(thread_dir_path, true);
            }
        }

        public static PostFormatter[] GetIndex(string board)
        {
            string board_folder = Path.Combine(Program.post_files_dir, board);

            DirectoryInfo info = new DirectoryInfo(board_folder);

            if (info.Exists)
            {
                DirectoryInfo[] folders = info.GetDirectories();

                List<PostFormatter> threads_pf = new List<PostFormatter>(folders.Length);

                for (int i = 0; i < folders.Count(); i++)
                {
                    string op_file = Path.Combine(folders[i].FullName, "op.json");
                    string optimized = Path.Combine(folders[i].FullName, folders[i].Name + "-opt.json");

                    if (File.Exists(op_file))
                    {
                        threads_pf.Add(load_post_data_str(File.ReadAllText(op_file), true));
                    }
                    else if (File.Exists(optimized))
                    {
                        try
                        {
                            Dictionary<string, object> t = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(optimized));
                            if (t.ContainsKey("op"))
                            {
                                threads_pf.Add(load_post_data_str(t["op"].ToString(), true));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Cannot decode optimized thread file {0}-{1}: {2}", board, folders[i].Name, ex.Message);
                        }
                    }
                }

                return threads_pf.OrderByDescending(x => x.PostID).ToArray();
            }
            else
            {
                return new PostFormatter[] { };
            }
        }

        private static PostFormatter load_post_data_str(string data, bool isop)
        {
            Dictionary<string, object> post_data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

            PostFormatter pf = new PostFormatter();

            if (post_data.ContainsKey("RawComment"))
            {
                pf.Comment = Convert.ToString(post_data["RawComment"]);
            }

            if (post_data.ContainsKey("Email"))
            {
                pf.Email = Convert.ToString(post_data["Email"]);
            }
            if (post_data.ContainsKey("Name"))
            {
                pf.Name = Convert.ToString(post_data["Name"]);
            }

            if (post_data.ContainsKey("PosterID"))
            {
                pf.PosterID = Convert.ToString(post_data["PosterID"]);
            }

            if (post_data.ContainsKey("Subject"))
            {
                pf.Subject = Convert.ToString(post_data["Subject"]);
            }
            if (post_data.ContainsKey("Trip"))
            {
                pf.Trip = Convert.ToString(post_data["Trip"]);
            }

            if (post_data.ContainsKey("ID"))
            {
                pf.PostID = Convert.ToInt32(post_data["ID"]);
            }

            if (post_data.ContainsKey("Time"))
            {
                pf.Time = Convert.ToDateTime(post_data["Time"]);
            }

            if (post_data.ContainsKey("FileHash"))
            {
                FileFormatter f = new FileFormatter();

                f.PostID = pf.PostID;

                if (post_data.ContainsKey("FileName"))
                {
                    f.FileName = Convert.ToString(post_data["FileName"]);
                }

                if (post_data.ContainsKey("FileHash"))
                {
                    f.Hash = Convert.ToString(post_data["FileHash"]);
                }
                if (post_data.ContainsKey("ThumbTime"))
                {
                    f.ThumbName = Convert.ToString(post_data["ThumbTime"]);
                }

                if (post_data.ContainsKey("FileHeight"))
                {
                    f.Height = Convert.ToInt32(post_data["FileHeight"]);
                }

                if (post_data.ContainsKey("FileWidth"))
                {
                    f.Width = Convert.ToInt32(post_data["FileWidth"]);
                }

                if (post_data.ContainsKey("FileSize"))
                {
                    f.Size = Convert.ToInt32(post_data["FileSize"]);
                }

                pf.MyFile = f;
            }

            if (isop)
            {
                if (post_data.ContainsKey("Closed"))
                {
                    pf.IsLocked = Convert.ToBoolean(post_data["Closed"]);
                }

                if (post_data.ContainsKey("Sticky"))
                {
                    pf.IsSticky = Convert.ToBoolean(post_data["Sticky"]);
                }

                pf.Type = PostFormatter.PostType.OP;
            }
            else
            {
                pf.Type = PostFormatter.PostType.Reply;
            }

            return pf;
        }
    }
}
