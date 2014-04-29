using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver
{
    public static class FileSystemStats
    {

        private static string[] dirs_to_watch = 
        {
            Program.thumb_save_dir, 
            Program.file_save_dir,
            Program.api_cache_dir,
            Program.post_files_dir//,Program.temp_files_dir
        };


        private static Dictionary<string, DirectoryStatsEntry> dirs = new Dictionary<string, DirectoryStatsEntry>();

        public static void Init()
        {
            System.Threading.Tasks.Task.Factory.StartNew((Action)delegate
            {
                foreach (string dir in dirs_to_watch)
                {
                    try { prepare_dir(dir); }
                    catch (Exception) { }
                }
            });
        }

        public static void Dispose()
        {
            foreach (DirectoryStatsEntry a in dirs.Values) { a.Dispose(); }
        }

        public static double TotalUsage
        {
            get
            {
                string[] keys = dirs.Keys.ToArray();
                double f = 0;
                foreach (string key in keys)
                {
                    f += dirs[key].TotalSize;
                }
                return f;
            }
        }

        public static bool IsSaveDirDriveLowOnDiskSpace
        {
            get
            {
                try
                {
                    DirectoryInfo e = new DirectoryInfo(Program.program_dir);

                    foreach (DriveInfo drive in DriveInfo.GetDrives())
                    {
                        if (drive.RootDirectory.FullName == e.Root.FullName)
                        {
                            return (drive.AvailableFreeSpace / 1024 / 1024 / 1024) < 1.0;
                        }
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }


            }
        }

        private static DirectoryStatsEntry prepare_dir(string dir)
        {
            if (!dirs.ContainsKey(dir))
            {
                DirectoryStatsEntry r = new DirectoryStatsEntry(dir);
                if (r.Exist)
                {
                    dirs.Add(dir, r);
                    return r;
                }
                else
                {
                    return r;
                }
            }
            else { return dirs[dir]; }
        }

        public static DirectoryStatsEntry GetDirStats(string path)
        {
            if (dirs.ContainsKey(path))
            {
                return dirs[path];
            }
            else
            {
                return prepare_dir(path);
            }
        }
    }

    public class DirectoryStatsEntry : IDisposable
    {

        private FileSystemWatcher my_watcher;

        private Dictionary<string, long> added_files;

        public DirectoryStatsEntry(string path)
        {
            this.Path = path;
            this.Exist = Directory.Exists(path);

            if (this.Exist)
            {
                DirectoryInfo df = new DirectoryInfo(path);

                FileInfo[] All_Files = df.GetFiles("*", SearchOption.AllDirectories);

                this.FileCount = All_Files.Length;

                added_files = new Dictionary<string, long>(this.FileCount);

                IOrderedEnumerable<FileInfo> sorted = All_Files.OrderBy(x => x.Length); // ;_; poor poor poor performance

                for (int index = 0; index < this.FileCount; index++)
                {
                    FileInfo fifo = All_Files[index];
                    this.TotalSize += fifo.Length;
                    if (!added_files.ContainsKey(fifo.FullName)) { added_files.Add(fifo.FullName, fifo.Length); }
                }

                if (sorted.Count() > 0)
                {
                    this.LargestFile = sorted.Last().Length;
                    this.SmallestFile = sorted.First().Length;
                }
                else
                {
                    this.LargestFile = 0;
                    this.SmallestFile = 0;
                }

                my_watcher = new FileSystemWatcher(path);
                my_watcher.EnableRaisingEvents = true;
                my_watcher.IncludeSubdirectories = true;
                my_watcher.Created += this.my_watcher_Created;
                my_watcher.Deleted += this.my_watcher_Deleted;
                my_watcher.Changed += this.my_watcher_Changed;
            }
        }

        void my_watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                FileInfo file = new FileInfo(e.FullPath);
                if (file.Exists)
                {
                    if (added_files.ContainsKey(e.FullPath))
                    {
                        long s = added_files[e.FullPath];
                        this.TotalSize -= s;
                        added_files[e.FullPath] = file.Length;
                        this.TotalSize += file.Length;
                    }
                    else
                    {
                        added_files.Add(e.FullPath, file.Length);
                        this.TotalSize += file.Length;
                    }
                }
            }
            catch (Exception)
            { }
        }

        void my_watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (added_files.ContainsKey(e.FullPath))
                {
                    long file_size = added_files[e.FullPath];

                    added_files.Remove(e.FullPath);

                    if (file_size == this.SmallestFile || file_size == this.LargestFile)
                    {
                        var sorted = this.added_files.OrderBy(x => x.Value);

                        if (sorted.Count() > 0)
                        {
                            this.LargestFile = sorted.Last().Value;
                            this.SmallestFile = sorted.First().Value;
                        }
                    }

                    this.TotalSize -= file_size;
                    this.FileCount--;
                }
                else
                {
                    //sigh
                    this.FileCount--;
                    if (this.FileCount < 0) { this.FileCount = 0; }
                }
            }
            catch (Exception) { }
        }

        void my_watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                FileInfo file = new FileInfo(e.FullPath);

                if (file.Exists)
                {
                    this.TotalSize += file.Length;
                    this.FileCount++;

                    if (file.Length < SmallestFile)
                    {
                        this.SmallestFile = file.Length;
                    }

                    if (file.Length > this.LargestFile)
                    {
                        this.SmallestFile = file.Length;
                    }

                    if (!added_files.ContainsKey(file.FullName)) { added_files.Add(file.FullName, file.Length); }
                }
            }
            catch (Exception)
            {
            }
        }

        public bool Exist { get; private set; }

        public string Path { get; private set; }

        public double TotalSize { get; private set; }

        public double SmallestFile { get; private set; }

        public double LargestFile { get; private set; }

        public double AverageFileSize
        {
            get
            {
                if (this.FileCount > 0)
                {
                    return this.TotalSize / this.FileCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int FileCount { get; private set; }

        public void Dispose()
        {
            if (my_watcher != null)
            {
                my_watcher.EnableRaisingEvents = false;
                my_watcher.Deleted -= this.my_watcher_Deleted;
                my_watcher.Created -= this.my_watcher_Created;
                my_watcher.Changed -= this.my_watcher_Changed;
                my_watcher.Dispose();
                my_watcher = null;
                GC.Collect();
            }
        }

    }
}
