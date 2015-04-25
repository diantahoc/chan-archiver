using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Net;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using AniWrap.DataTypes;

namespace ChanArchiver
{
    public struct FoolFuukaParserData
    {
        public string HTML_URL;
        public string HOST;
        public string BOARD;
        public int ThreadID;
    }

    public static class FoolFuukaParser
    {
        private static string get_data(string url)
        {
            using (System.Net.WebClient wv = new System.Net.WebClient())
            {
                wv.Headers.Add("user-agent", "Mozilla/18.0 (compatible; MSIE 10.0; Windows NT 5.2; .NET CLR 3.5.3705;)");

                byte[] data = wv.DownloadData(url);

                try
                {
                    byte[] un_data = Decompress(data);

                    return System.Text.Encoding.UTF8.GetString(un_data);
                }
                catch (Exception)
                {
                    return System.Text.Encoding.UTF8.GetString(data);
                }
            }
        }

        private static byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public static ThreadContainer Parse(FoolFuukaParserData data)
        {
            try
            {
                return parse_ffuuka_json(data);
            }
            catch (Exception ex)
            {
                if (ex.Message == "404")
                {
                    return null;
                    //return parse_html(archive, board, threadID);
                }
                else
                {
                    return null;
                }
            }
        }

        private static string fetch_api(FoolFuukaParserData ffp_data)
        {
            string url = string.Format("http://{0}/_/api/chan/thread/?board={1}&num={2}", ffp_data.HOST, ffp_data.BOARD, ffp_data.ThreadID);

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
          
            wr.AllowAutoRedirect = true;
            wr.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:26.0) Gecko/20100101 Firefox/26.0";
            wr.Referer = string.Format("http://boards.4chan.org/{0}/res/{1}", ffp_data.BOARD, ffp_data.ThreadID);

            wr.Method = "GET";

            byte[] data = null;

            using (WebResponse wbr = wr.GetResponse())
            {
                using (Stream s = wbr.GetResponseStream())
                {
                    using (MemoryStream memio = new MemoryStream())
                    {
                        s.CopyTo(memio);
                        data = memio.ToArray();
                    }
                }
            }

            try
            {
                byte[] uncompressed = Decompress(data);
                return System.Text.Encoding.UTF8.GetString(uncompressed);
            }
            catch (Exception)
            {
                return System.Text.Encoding.UTF8.GetString(data);
            }
        }

        private static ThreadContainer parse_ffuuka_json(FoolFuukaParserData ffp_data)
        {
            ThreadContainer tc = null;

            string data = fetch_api(ffp_data);

            JsonObject ob = JsonConvert.Import<JsonObject>(data);

            JsonObject thread_object = (JsonObject)ob[ffp_data.ThreadID.ToString()];

            JsonObject op_post = (JsonObject)thread_object["op"];

            tc = new ThreadContainer(parse_thread(op_post, ffp_data));

            JsonArray replies = (JsonArray)thread_object["posts"];

            foreach (object t in replies)
            {
                continue;
                //tc.AddReply(parse_reply(a, ffp_data));
            }

            return tc;
        }

        private static Thread parse_thread(JsonObject data, FoolFuukaParserData ffp_data)
        {
            Thread t = new Thread();

            t.OwnerThread = t;

            t.Board = ffp_data.BOARD;

            t.ID = Convert.ToInt32(data["thread_num"]);

            if (data["comment_processed"] != null)
            {
                t.Comment = data["comment_processed"].ToString(); // raw html comment
            }

            if (data["email"] != null)
            {
                t.Email = data["email"].ToString();
            }

            if (data["media"] != null)
            {
                t.File = parse_file(data, ffp_data, t);
            }

            if (data["title"] != null)
            {
                t.Subject = data["title"].ToString();
            }

            if (data["capcode"] != null)
            {
                switch (data["capcode"].ToString())
                {
                    case "N":
                        t.Capcode = GenericPost.CapcodeEnum.None;
                        break;
                    default:
                        t.Capcode = GenericPost.CapcodeEnum.None;
                        break;
                }
            }

            if (data["sticky"] != null)
            {
                t.IsSticky = (data["sticky"].ToString() == "1");
            }

            if (data["name"] != null)
            {
                t.Name = data["name"].ToString();
            }

            if (data["trip"] != null)
            {
                t.Trip = data["trip"].ToString();
            }

            t.Time = AniWrap.Common.ParseUTC_Stamp(Convert.ToInt32(data["timestamp"]));

            return t;
        }

        private static GenericPost parse_reply(JsonObject data, FoolFuukaParserData ffp_data)
        {
            GenericPost gp = new GenericPost();

            gp.Board = ffp_data.BOARD;

            gp.ID = Convert.ToInt32(data["num"]);

            if (data["comment_processed"] != null)
            {
                gp.Comment = data["comment_processed"].ToString();
            }

            if (data["email"] != null)
            {
                gp.Email = data["email"].ToString();
            }

            if (data["title"] != null)
            {
                gp.Subject = data["title"].ToString();
            }

            if (data["media"] != null)
            {
                gp.File = parse_file(data, ffp_data, gp);
            }

            if (data["capcode"] != null)
            {
                switch (data["capcode"].ToString())
                {
                    case "N":
                        gp.Capcode = GenericPost.CapcodeEnum.None;
                        break;
                    default:
                        gp.Capcode = GenericPost.CapcodeEnum.None;
                        break;
                }
            }

            if (data["name"] != null)
            {
                gp.Name = data["name"].ToString();
            }

            if (data["trip"] != null)
            {
                gp.Trip = data["trip"].ToString();
            }

            gp.Time = AniWrap.Common.ParseUTC_Stamp(Convert.ToInt32(data["timestamp"]));

            return gp;
        }

        private static PostFile parse_file(JsonObject data, FoolFuukaParserData ffp_data, GenericPost owner)
        {
            if (data["media"] != null)
            {
                JsonObject media = (JsonObject)data["media"];
                if (media.Count == 0) { return null; }
                if (media["banned"].ToString() != "0") { return null; }
                if (media["media_status"].ToString() == "not-available") { return null; }

                PostFile pf = new PostFile();

                pf.board = ffp_data.BOARD;
                pf.filename = media["media_filename_processed"].ToString();

                pf.ext = pf.filename.Split('.').Last();
                pf.filename = pf.filename.Split('.').First();

                string thumb_link = media["thumb_link"].ToString();

                string media_link = media["media_link"].ToString();

                if (string.IsNullOrEmpty(media_link))
                {
                    return null;
                }

                pf.OverrideFileLinks(thumb_link, media_link);

                pf.hash = media["media_hash"].ToString();

                pf.height = Convert.ToInt32(media["media_h"]);
                pf.width = Convert.ToInt32(media["media_w"]);

                if (media["spoiler"] != null) { pf.IsSpoiler = (media["spoiler"].ToString() != "0"); }

                pf.thumbH = Convert.ToInt32(media["preview_h"]);
                pf.thumbW = Convert.ToInt32(media["preview_w"]);

                pf.size = Convert.ToInt32(media["media_size"]);

                pf.thumbnail_tim = media["media"].ToString().Split('.').First();
                pf.owner = owner;
                return pf;
            }
            else { return null; }
        }

    }
}
