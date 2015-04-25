using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Xml;
using AniWrap.DataTypes;
using AniWrap.Helpers;
using Jayrock.Json;
using Jayrock.Json.Conversion;

namespace AniWrap
{
    public class AniWrap
    {
        private CacheProvider cache;

        public AniWrap()
        {
            cache = new MemoryCacheProvdier();
        }

        public AniWrap(string cache_dir)
        {
            cache = new DiskCacheProvider(cache_dir);
        }

        public AniWrap(CacheProvider cache)
        {
            if (cache != null)
            {
                this.cache = cache;
            }
            else
            {
                this.cache = new MemoryCacheProvdier();
            }
        }

        #region Data Parsers
        /// <summary>
        /// Download the catalog.json file and parse it into an array of pages that contain an array of CatalogItem
        /// Pages contain an array of catalogs items.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="web_progress"></param>
        /// <returns></returns>
        /// 
        public CatalogItem[][] GetCatalog(string board)
        {
            APIResponse response = LoadAPI(string.Format("{0}://a.4cdn.org/{1}/catalog.json", Common.HttpPrefix, board));

            switch (response.Error)
            {
                case APIResponse.ErrorType.NoError:

                    List<CatalogItem[]> il = new List<CatalogItem[]>();

                    JsonArray list = JsonConvert.Import<JsonArray>(response.Data);

                    //p is page index
                    //u is thread index

                    for (int p = 0; p < list.Count; p++)
                    {
                        JsonObject page = (JsonObject)list[p];
                        List<CatalogItem> Unipage = new List<CatalogItem>();

                        JsonArray threads = (JsonArray)page["threads"];

                        for (int u = 0; u < threads.Count; u++)
                        {
                            JsonObject thread = (JsonObject)threads[u];
                            Unipage.Add(ParseJToken_Catalog(thread, p, board));
                        }

                        il.Add(Unipage.ToArray());

                        Unipage = null;
                    }

                    return il.ToArray();

                case APIResponse.ErrorType.NotFound:
                    throw new Exception("404");

                case APIResponse.ErrorType.Other:
                    throw new Exception(response.Data);

                default:
                    return null;
            }

        }

        public ThreadContainer GetThreadData(string board, int id)
        {
            APIResponse response = LoadAPI(string.Format("{0}://a.4cdn.org/{1}/thread/{2}.json", Common.HttpPrefix, board, id));

            switch (response.Error)
            {
                case APIResponse.ErrorType.NoError:
                    ThreadContainer tc = null;

                    JsonObject list = JsonConvert.Import<JsonObject>(response.Data);

                    //if (list == null) 
                    //{
                    //    FlushAPI(string.Format("http://a.4cdn.org/{0}/thread/{1}.json", board, id));
                    //    return GetThreadData(board, id);
                    //}

                    if (list.Names.Cast<string>().Contains("posts"))
                    {
                        JsonArray data = list["posts"] as JsonArray;

                        tc = new ThreadContainer(ParseThread((JsonObject)data.First(), board));

                        for (int index = 1; index < data.Count; index++)
                        {
                            tc.AddReply(ParseReply((JsonObject)data[index], board));
                        }
                    }

                    return tc;

                case APIResponse.ErrorType.NotFound:
                    throw new Exception("404");

                case APIResponse.ErrorType.Other:
                    throw new Exception(response.Data);

                default:
                    return null;
            }
        }

        private Thread ParseThread(JsonObject data, string board)
        {
            Thread t = new Thread();

            t.Board = board;

            //comment
            if (data["com"] != null)
            {
                t.Comment = data["com"].ToString();
            }
            else
            {
                t.Comment = "";
            }

            //mail
            if (data["email"] != null)
            {
                t.Email = HttpUtility.HtmlDecode(data["email"].ToString());
            }
            else
            {
                t.Email = "";
            }

            //poster name
            if (data["name"] != null)
            {
                t.Name = HttpUtility.HtmlDecode(data["name"].ToString());
            }
            else
            {
                t.Name = "";
            }

            //subject
            if (data["sub"] != null)
            {
                t.Subject = HttpUtility.HtmlDecode(data["sub"].ToString());
            }
            else
            {
                t.Subject = "";
            }

            if (data["trip"] != null)
            {
                t.Trip = data["trip"].ToString();
            }
            else
            {
                t.Trip = "";
            }

            if (data["id"] != null)
            {
                t.PosterID = data["id"].ToString();
            }
            else
            {
                t.PosterID = "";
            }

            if (data["capcode"] != null)
            {
                t.Capcode = parse_capcode(Convert.ToString(data["capcode"]));
            }

            if (data["sticky"] != null)
            {
                t.IsSticky = Convert.ToInt32(data["sticky"]) == 1;
            }

            if (data["closed"] != null)
            {
                t.IsClosed = Convert.ToInt32(data["closed"]) == 1;
            }

            if (data["archived"] != null)
            {
                t.IsArchived = Convert.ToInt32(data["archived"]) == 1;
            }

            if (data["country"] != null)
            {
                t.country_flag = data["country"].ToString();
            }
            else
            {
                t.country_flag = "";
            }

            if (data["country_name"] != null)
            {
                t.country_name = data["country_name"].ToString();
            }
            else
            {
                t.country_name = "";
            }

            t.File = ParseFile(data, board);

            if (t.File != null) { t.File.owner = t; }

            t.image_replies = Convert.ToInt32(data["images"]);

            t.ID = Convert.ToInt32(data["no"]);

            t.text_replies = Convert.ToInt32(data["replies"]);
            t.Time = Common.ParseUTC_Stamp(Convert.ToInt32(data["time"]));

            return t;
        }

        private PostFile ParseFile(JsonObject data, string board)
        {
            if (data["filename"] != null)
            {
                PostFile pf = new PostFile();
                pf.filename = HttpUtility.HtmlDecode(data["filename"].ToString());
                pf.ext = data["ext"].ToString().Substring(1);
                pf.height = Convert.ToInt32(data["h"]);
                pf.width = Convert.ToInt32(data["w"]);
                pf.thumbW = Convert.ToInt32(data["tn_w"]);
                pf.thumbH = Convert.ToInt32(data["tn_h"]);
                pf.thumbnail_tim = data["tim"].ToString();
                pf.board = board;
                pf.hash = data["md5"].ToString();
                pf.size = Convert.ToInt32(data["fsize"]);
                if (data["spoiler"] != null)
                {
                    pf.IsSpoiler = Convert.ToInt32(data["spoiler"]) == 1;
                }
                return pf;
            }
            else
            {
                return null;
            }
        }

        private GenericPost ParseReply(JsonObject data, string board)
        {
            GenericPost t = new GenericPost();

            t.Board = board;

            //comment
            if (data["com"] != null)
            {
                t.Comment = data["com"].ToString();
            }
            else
            {
                t.Comment = "";
            }

            //mail
            if (data["email"] != null)
            {
                t.Email = HttpUtility.HtmlDecode(data["email"].ToString());
            }
            else
            {
                t.Email = "";
            }

            //poster name
            if (data["name"] != null)
            {
                t.Name = HttpUtility.HtmlDecode(data["name"].ToString());
            }
            else
            {
                t.Name = "";
            }

            //subject
            if (data["sub"] != null)
            {
                t.Subject = HttpUtility.HtmlDecode(data["sub"].ToString());
            }
            else
            {
                t.Subject = "";
            }

            if (data["trip"] != null)
            {
                t.Trip = data["trip"].ToString();
            }
            else
            {
                t.Trip = "";
            }

            if (data["id"] != null)
            {
                t.PosterID = data["id"].ToString();
            }
            else
            {
                t.PosterID = "";
            }

            if (data["country"] != null)
            {
                t.country_flag = data["country"].ToString();
            }
            else
            {
                t.country_flag = "";
            }

            if (data["country_name"] != null)
            {
                t.country_name = data["country_name"].ToString();
            }
            else
            {
                t.country_name = "";
            }

            if (data["capcode"] != null)
            {
                t.Capcode = parse_capcode(Convert.ToString(data["capcode"]));
            }

            t.File = ParseFile(data, board);

            if (t.File != null) { t.File.owner = t; }

            t.ID = Convert.ToInt32(data["no"]); ;

            t.Time = Common.ParseUTC_Stamp(Convert.ToInt32((data["time"])));

            return t;
        }

        private GenericPost.CapcodeEnum parse_capcode(string cap)
        {
            switch (cap.ToLower())
            {
                /*none, mod, admin, admin_highlight, developer*/
                case "admin":
                case "admin_highlight":
                    return GenericPost.CapcodeEnum.Admin;
                case "developer":
                    return GenericPost.CapcodeEnum.Developer;
                case "mod":
                    return GenericPost.CapcodeEnum.Mod;
                default:
                    return GenericPost.CapcodeEnum.None;
            }
        }

        private CatalogItem ParseJToken_Catalog(JsonObject thread, int pagenumber, string board)
        {
            CatalogItem ci = new CatalogItem();

            //post number - no
            ci.ID = Convert.ToInt32(thread["no"]);

            // post time - now
            ci.Time = Common.ParseUTC_Stamp(Convert.ToInt32(thread["time"]));

            //name 
            if (thread["name"] != null)
            {
                ci.Name = thread["name"].ToString();
            }
            else
            {
                ci.Name = "";
            }

            if (thread["com"] != null)
            {
                ci.Comment = thread["com"].ToString();
            }
            else
            {
                ci.Comment = "";
            }

            if (thread["trip"] != null)
            {
                ci.Trip = thread["trip"].ToString();
            }
            else
            {
                ci.Trip = "";
            }

            if (thread["id"] != null)
            {
                ci.PosterID = thread["id"].ToString();
            }
            else
            {
                ci.PosterID = "";
            }

            if (thread["filename"] != null)
            {
                PostFile pf = new PostFile();
                pf.filename = thread["filename"].ToString();
                pf.ext = thread["ext"].ToString().Replace(".", "");
                pf.height = Convert.ToInt32(thread["h"]);
                pf.width = Convert.ToInt32(thread["w"]);
                pf.thumbW = Convert.ToInt32(thread["tn_w"]);
                pf.thumbH = Convert.ToInt32(thread["tn_h"]);
                pf.owner = ci;
                pf.thumbnail_tim = thread["tim"].ToString();
                pf.board = board;

                pf.hash = thread["md5"].ToString();
                pf.size = Convert.ToInt32(thread["fsize"]);

                ci.File = pf;
            }

            if (thread["last_replies"] != null)
            {
                JsonArray li = (JsonArray)thread["last_replies"];

                List<GenericPost> repl = new List<GenericPost>();

                foreach (JsonObject j in li)
                {
                    repl.Add(ParseReply(j, board)); // HACK: parent must not be null.
                }

                ci.trails = repl.ToArray();
            }

            if (thread["bumplimit"] != null)
            {
                ci.BumpLimit = Convert.ToInt32(thread["bumplimit"]);
            }
            else
            {
                ci.BumpLimit = 300; //most common one
            }

            if (thread["imagelimit"] != null)
            {
                ci.ImageLimit = Convert.ToInt32(thread["imagelimit"]);
            }
            else
            {
                ci.ImageLimit = 150;
            }

            ci.image_replies = Convert.ToInt32(thread["images"]);
            ci.text_replies = Convert.ToInt32(thread["replies"]);
            ci.page_number = pagenumber;

            return ci;
            /*{
           "tim": 1385141348984,
           "time": 1385141348,
           "resto": 0,
           "bumplimit": 0,
           "imagelimit": 0,
           "omitted_posts": 1,
           "omitted_images": 0,*/
        }

        public Dictionary<int, DateTime> GetBoardThreadsID(string board)
        {
            APIResponse response = LoadAPI(string.Format("{0}://a.4cdn.org/{1}/threads.json", Common.HttpPrefix, board));

            switch (response.Error)
            {
                case APIResponse.ErrorType.NoError:

                    Dictionary<int, DateTime> dic = new Dictionary<int, DateTime>();

                    JsonArray pages = JsonConvert.Import<JsonArray>(response.Data);

                    for (int i = 0; i < pages.Count; i++)
                    {
                        JsonObject page = (JsonObject)pages[i];
                        JsonArray threads = (JsonArray)page["threads"];

                        foreach (JsonObject threadinfo in threads)
                        {
                            dic.Add(Convert.ToInt32(threadinfo["no"]),
                                Common.ParseUTC_Stamp(Convert.ToInt32(threadinfo["last_modified"])));
                        }
                    }


                    return dic;

                case APIResponse.ErrorType.NotFound:
                    throw new Exception("404");

                case APIResponse.ErrorType.Other:
                    throw new Exception(response.Data);

                default:
                    return null;
            }
        }

        public struct BoardInfo
        {
            public string Title { get; set; }
            public int BumpLimit { get; set; }
            public int ImageLimit { get; set; }
        }

        public Dictionary<string, BoardInfo> GetAvailableBoards()
        {
            string data = ChanArchiver.Properties.Resources.cached_boards;

            StorageEntry cached_catalog_data = cache.GetText("CatalogData");

            if (cached_catalog_data == null || cached_catalog_data != null && (DateTime.Now - cached_catalog_data.LastModified).Days > 6)
            {
                APIResponse api_r = LoadAPI(string.Format("{0}://a.4cdn.org/boards.json", Common.HttpPrefix));

                if (api_r.Error == APIResponse.ErrorType.NoError)
                {
                    data = api_r.Data;

                    cache.StoreText("CatalogData", api_r.Data, DateTime.Now);
                }
            }
            else
            {
                data = cached_catalog_data.Text;
            }

            JsonObject json = JsonConvert.Import<JsonObject>(data);

            JsonArray boards = (JsonArray)json["boards"];

            var dic = new Dictionary<string, BoardInfo>();

            for (int i = 0; i < boards.Count; i++)
            {
                JsonObject board = (JsonObject)boards[i];

                string letter = Convert.ToString(board["board"]);
                string desc = Convert.ToString(board["title"]);
                int bl; int iml;
                if (letter == "f")
                {
                    bl = 300; iml = 150;
                }
                else
                {
                    bl = Convert.ToInt32(board["bump_limit"]);
                    iml = Convert.ToInt32(board["image_limit"]);
                }
                dic.Add(letter, new BoardInfo()
                {
                    Title = desc,
                    BumpLimit = bl,
                    ImageLimit = iml
                });
            }

            return dic;
        }

        #endregion

        private static readonly DateTime old_date = DateTime.Now.Subtract(new TimeSpan(365, 0, 0, 0));

        private APIResponse LoadAPI(string url)
        {
            DateTime lastModified = old_date;

            APIResponse result = null;

            StorageEntry se = cache.GetText(url);

            if (se != null)
            {
                lastModified = se.LastModified;
            }

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(url);

            wr.IfModifiedSince = lastModified;

            wr.UserAgent = ChanArchiver.Program.get_random_user_agent();

            WebResponse wbr = null;

            try
            {
                byte[] data;

                wbr = wr.GetResponse();

                using (Stream s = wbr.GetResponseStream())
                {
                    int iByteSize = 0;

                    byte[] byteBuffer = new byte[2048];

                    using (MemoryStream MemIo = new MemoryStream())
                    {
                        while ((iByteSize = s.Read(byteBuffer, 0, 2048)) > 0)
                        {
                            MemIo.Write(byteBuffer, 0, iByteSize);
                            ChanArchiver.NetworkUsageCounter.Add_ApiConsumed(iByteSize);
                        }
                        data = MemIo.ToArray();
                    }
                }

                string text = System.Text.Encoding.UTF8.GetString(data.ToArray());

                string lm = wbr.Headers["Last-Modified"];

                DateTime lmm = DateTime.Parse(lm);

                cache.StoreText(url, text, lmm);

                result = new APIResponse(text, APIResponse.ErrorType.NoError);

            }
            catch (WebException wex)
            {
                HttpWebResponse httpResponse = wex.Response as HttpWebResponse;
                if (httpResponse != null)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.NotModified)
                    {
                        if (se != null)
                        {
                            result = new APIResponse(se.Text, APIResponse.ErrorType.NoError);
                        }
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        result = new APIResponse(null, APIResponse.ErrorType.NotFound);
                        //delete cache entry since resource has 404'ed
                        cache.ClearText(url);
                    }
                    else
                    {
                        result = new APIResponse(wex.Message, APIResponse.ErrorType.Other);
                        //throw wex;
                    }
                }
                else
                {
                    result = new APIResponse(wex.Message, APIResponse.ErrorType.Other);
                    //throw wex;
                }
            }

            if (wbr != null)
            {
                wbr.Close();
            }

            return result;
        }
    }
}
