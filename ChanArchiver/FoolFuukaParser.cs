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
    public class FoolFuukaParserData
    {
        public FoolFuukaParserData(ArchiveInfo info, string board, int threadId)
        {
            if (info != null && info.Software != ArchiveInfo.ArchiverSoftware.FoolFuuka)
            {
                throw new ArgumentException("ArchiveInfo Software must be FoolFuuka");
            }

            this.Archive = info;
            this.BOARD = board;
            this.ThreadID = threadId;
        }

        public ArchiveInfo Archive { get; private set; }

        public string HOST { get { return Archive.Domain; } }

        public string BOARD { get; private set; }
        public int ThreadID { get; private set; }

        private string get_http_prefix()
        {
            /*
             Check user preference first
             * then check archive cabability
             */
            bool user_want_https = Settings.UseHttps;
            bool user_want_http = !user_want_https;

            if (user_want_https && this.Archive.SupportHttps)
            {
                return "https";
            }
            else if (user_want_https && !this.Archive.SupportHttps)
            {
                return "http";
            }
            else if (user_want_http && !this.Archive.SupportHttp && this.Archive.SupportHttps)
            {
                return "https";
            }
            else
            {
                return "http";
            }
        }

        public string GetAPIUrl()
        {
            return string.Format("{0}://{1}/_/api/chan/thread/?board={2}&num={3}",
                get_http_prefix(), this.HOST, this.BOARD, this.ThreadID);
        }

        public string GetBoardIndexUrl()
        {
            return string.Format("{0}://{1}/{2}/", get_http_prefix(), this.HOST, this.BOARD);
        }
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
                    //TODO parse_html
                    return null;
                }
            }
        }

        private static string fetch_api(FoolFuukaParserData ffp_data)
        {
            string url = ffp_data.GetAPIUrl();

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);

            wr.CookieContainer = get_cookies_for_board(ffp_data);
            wr.AllowAutoRedirect = true;
            wr.UserAgent = Program.get_random_user_agent();
            //wr.Referer = string.Format("http://boards.4chan.org/{0}/thread/{1}", ffp_data.BOARD, ffp_data.ThreadID);

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


        private static CookieContainer get_cookies_for_board(FoolFuukaParserData ffp_data)
        {
            try 
            {
                string url = ffp_data.GetBoardIndexUrl();

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);

                wr.CookieContainer = new CookieContainer();

                using (var w = wr.GetResponse()) 
                {
                    return wr.CookieContainer;
                }
            }
            catch
            {
                return new CookieContainer();
            }
        }

        private static ThreadContainer parse_ffuuka_json(FoolFuukaParserData ffp_data)
        {
            ThreadContainer tc = null;

            string data = fetch_api(ffp_data);

            JsonObject response = JsonConvert.Import<JsonObject>(data);

            JsonObject threadObject = (JsonObject)response[ffp_data.ThreadID.ToString()];

            JsonObject opPost = (JsonObject)threadObject["op"];

            tc = new ThreadContainer(parse_thread(opPost, ffp_data));

            JsonObject postsObject = (JsonObject)threadObject["posts"];

            foreach (string reply_id in postsObject.Names.Cast<string>())
            {
                JsonObject replyObject = (JsonObject)postsObject[reply_id];
                GenericPost reply = parse_reply(replyObject, ffp_data);
                tc.AddReply(reply);
                continue;
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
