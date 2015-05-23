using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace AniWrap
{
    public interface CacheProvider
    {
        void StoreText(string url, string text, DateTime lastModified);
        StorageEntry GetText(string url);
        void ClearText(string url);
    }

    public class StorageEntry
    {
        public DateTime LastModified;
        public string Text;
    }


    public class MemoryCacheProvdier : CacheProvider
    {
        private Dictionary<string, StorageEntry> data = new Dictionary<string, StorageEntry>();

        public void StoreText(string url, string text, DateTime lastModified)
        {
            if (data.ContainsKey(url))
            {
                data[url] = new StorageEntry() { LastModified = lastModified, Text = text };
            }
            else
            {
                data.Add(url, new StorageEntry() { LastModified = lastModified, Text = text });
            }
        }

        public StorageEntry GetText(string url)
        {
            if (data.ContainsKey(url))
            {
                return data[url];
            }
            else
            {
                return null;
            }
        }


        public void ClearText(string url)
        {
            if (data.ContainsKey(url))
                data.Remove(url);
        }
    }

    public class DiskCacheProvider : CacheProvider
    {
        string storage_dir;

        public DiskCacheProvider()
        {
            storage_dir = Path.Combine(Path.GetTempPath(), "__cache_dir_dcp" + DateTime.Now.ToFileTime().ToString());
            Directory.CreateDirectory(storage_dir);
        }

        public DiskCacheProvider(string directory)
        {
            storage_dir = directory;
            Directory.CreateDirectory(storage_dir);
        }

        public void StoreText(string url, string text, DateTime lastModified)
        {
            string file_path = Path.Combine(storage_dir, Common.MD5(url));

            using (TextWriter tw = File.CreateText(file_path))
            {
                using (Jayrock.Json.JsonTextWriter jtw = new Jayrock.Json.JsonTextWriter(tw))
                {
                    jtw.WriteStartObject();
                    jtw.WriteMember("LastModified");
                    jtw.WriteString(datetime_tostring(lastModified));

                    jtw.WriteMember("Text");
                    jtw.WriteString(text);

                    jtw.WriteEndObject();
                }
            }
        }

        public StorageEntry GetText(string url)
        {
            string file_path = Path.Combine(storage_dir, Common.MD5(url));

            if (File.Exists(file_path))
            {
                try
                {
                    JsonObject obj = JsonConvert.Import<JsonObject>(File.ReadAllText(file_path));

                    StorageEntry se = new StorageEntry();

                    se.LastModified = parse_datetime(obj["LastModified"].ToString());

                    se.Text = obj["Text"].ToString();

                    return se;
                }
                catch (JsonException) 
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        static DateTime parse_datetime(string s)
        {
            return XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.Local);
        }

        static string datetime_tostring(DateTime s)
        {
            return XmlConvert.ToString(s, XmlDateTimeSerializationMode.Local);
        }


        public void ClearText(string url)
        {
            string file_path = Path.Combine(storage_dir, Common.MD5(url));
            if (File.Exists(file_path))
                File.Delete(file_path);
        }
    }

}
