using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using System.Net;

namespace ChanArchiver
{
    public static class ArchivesProvider
    {
        private static List<ArchiveInfo> archives = new List<ArchiveInfo>();

        const string update_url = "https://4chan-x.just-believe.in/json/archives.json";

        public static void Load()
        {
            Task.Factory.StartNew(new Action(update));
        }

        const string LastUpdate = "L";
        const string Data = "D";

        private static void update()
        {
            DateTime last_update = AniWrap.Common.UnixEpoch;

            if (File.Exists(FileSaveLocation))
            {
                try
                {
                    JsonObject jo = JsonConvert.Import<JsonObject>(File.ReadAllText(FileSaveLocation));

                    last_update = DateTime.Parse(jo[LastUpdate].ToString());

                    if ((DateTime.Now - last_update).Days <= 3)
                    {
                        string data = jo[Data].ToString();

                        handle_data(data);

                        return;
                    }
                }
                catch { }
            }

            try
            {
                using (WebClient wc = new WebClient())
                {
                    string data = wc.DownloadString(update_url);

                    handle_data(data);

                    Dictionary<string, object> d = new Dictionary<string, object>();
                    d.Add(LastUpdate, DateTime.Now.ToString());
                    d.Add(Data, data);

                    File.WriteAllText(FileSaveLocation, JsonConvert.ExportToString(d));
                }
            }
            catch { }
        }

        private static void handle_data(string data)
        {
            JsonArray ja = JsonConvert.Import<JsonArray>(data);

            foreach (JsonObject jo in ja)
            {
                archives.Add(ArchiveInfo.fromJson(jo));
            }
        }

        private static string FileSaveLocation
        {
            get
            {
                return Path.Combine(Program.program_dir, "archives.json");
            }
        }

        public static ArchiveInfo[] GetArchivesForBoard(string b, bool onlyfiles)
        {
            List<ArchiveInfo> a = new List<ArchiveInfo>();
            foreach (var c in GetAllArchives())
            {
                if (onlyfiles)
                {
                    if (c.IsFileSupported(b))
                    {
                        a.Add(c);
                    }
                }
                else
                {
                    if (c.IsBoardSupported(b))
                    {
                        a.Add(c);
                    }
                }
            }
            return a.ToArray();
        }

        public static ArchiveInfo GetArchiveForBoard(string b, bool onlyfiles)
        {
            foreach (var c in GetAllArchives())
            {
                if (onlyfiles)
                {
                    if (c.IsFileSupported(b))
                    {
                        return c;
                    }
                }
                else
                {
                    if (c.IsBoardSupported(b))
                    {
                        return c;
                    }
                }
            }
            return null;
        }

        public static IEnumerable<ArchiveInfo> GetAllArchives()
        {
            foreach (ArchiveInfo info in archives)
            {
                if (info.Software == ArchiveInfo.ArchiverSoftware.FoolFuuka)
                {
                    yield return info;
                }
            }
        }

    }

    public class ArchiveInfo
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public bool SupportHttp { get; set; }
        public bool SupportHttps { get; set; }
        public ArchiverSoftware Software { get; set; }

        private List<string> supported_board_posts;
        private List<string> supported_full_files;

        public ArchiveInfo()
        {
            this.supported_board_posts = new List<string>(5);
            this.supported_full_files = new List<string>(5);
        }

        public string[] SupportedBoards
        {
            get { return this.supported_board_posts.ToArray(); }
        }

        public string[] SupportedFiles
        {
            get { return this.supported_full_files.ToArray(); }
        }

        public bool IsBoardSupported(string board)
        {
            return this.supported_board_posts.Contains(board);
        }

        public bool IsFileSupported(string board)
        {
            return this.supported_full_files.Contains(board);
        }

        public void AddPostBoard(string b)
        {
            if (!this.supported_board_posts.Contains(b))
            {
                this.supported_board_posts.Add(b);
            }
        }

        public void AddFileBoard(string b)
        {
            if (!this.supported_full_files.Contains(b))
            {
                this.supported_full_files.Add(b);
            }
        }

        public enum ArchiverSoftware
        {
            FoolFuuka, Fuuka
        }

        public IEnumerable<string> GetSupportedBoards()
        {
            return this.supported_board_posts;
        }

        public IEnumerable<string> GetSupportedFiles()
        {
            return this.supported_full_files;
        }

        public static ArchiveInfo fromJson(JsonObject jo)
        {
            try
            {
                ArchiveInfo info = new ArchiveInfo()
                {
                    Name = jo["name"].ToString(),
                    Domain = jo["domain"].ToString(),
                    SupportHttp = Convert.ToBoolean(jo["http"]),
                    SupportHttps = Convert.ToBoolean(jo["https"]),
                    Software = "foolfuuka" == jo["software"].ToString() ? ArchiverSoftware.FoolFuuka : ArchiverSoftware.Fuuka
                };

                JsonArray boards = (JsonArray)jo["boards"];
                foreach (string b in boards.Cast<string>())
                {
                    info.AddPostBoard(b);
                }

                JsonArray files = (JsonArray)jo["files"];
                foreach (string b in boards.Cast<string>())
                {
                    info.AddFileBoard(b);
                }

                return info;
            }
            catch
            {
                return null;
            }
        }
    }
}
