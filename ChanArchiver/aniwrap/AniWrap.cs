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

namespace AniWrap
{
    public class AniWrap
    {
        private string _cache_dir;

        public AniWrap()
        {
            _cache_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), "aniwrap_cache");
            check_dir(_cache_dir);
        }

        public AniWrap(string cache_dir)
        {
            _cache_dir = cache_dir;
            check_dir(_cache_dir);
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
            APIResponse response = LoadAPI("http://a.4cdn.org/%/catalog.json".Replace("%", board));

            switch (response.Error)
            {
                case APIResponse.ErrorType.NoError:

                    List<CatalogItem[]> il = new List<CatalogItem[]>();

                    List<Dictionary<string, object>> list = (List<Dictionary<string, object>>)Newtonsoft.Json.JsonConvert.DeserializeObject(response.Data, typeof(List<Dictionary<string, object>>));
                    //p is page index
                    //u is thread index
                    for (int p = 0; p < list.Count(); p++)
                    {
                        Dictionary<string, object> page = list[p];
                        List<CatalogItem> Unipage = new List<CatalogItem>();

                        Newtonsoft.Json.Linq.JArray threads = (Newtonsoft.Json.Linq.JArray)page["threads"];

                        for (int u = 0; u < threads.Count; u++)
                        {
                            Newtonsoft.Json.Linq.JToken thread = threads[u];
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

        /*
        public ThreadContainer[] GetPage(string board, int page)
        {
            APIResponse response = LoadAPI("http://api.4chan.org/%/$.json".Replace("%", board).Replace("$", page.ToString()));

            switch (response.Error)
            {
                case APIResponse.ErrorType.NoError:
                    List<ThreadContainer> il = new List<ThreadContainer>();

                    Dictionary<string, object> list = (Dictionary<string, object>)Newtonsoft.Json.JsonConvert.DeserializeObject(response.Data, typeof(Dictionary<string, object>));

                    //u is thread index

                    Newtonsoft.Json.Linq.JArray threads = (Newtonsoft.Json.Linq.JArray)list["threads"];

                    for (int u = 0; u < threads.Count; u++)
                    {
                        Newtonsoft.Json.Linq.JToken posts_object = threads[u]; // array of 'posts' objects. Each one is an array of {Main threads + last replies}.
                        Newtonsoft.Json.Linq.JToken posts_property = posts_object["posts"];

                        //first one is 0 -- > a thread
                        //the rest is the last replies

                        ThreadContainer tc = new ThreadContainer(ParseThread(posts_property[0], board));

                        for (int post_index = 1; post_index < posts_property.Count(); post_index++)
                        {
                            Newtonsoft.Json.Linq.JToken single_post = posts_property[post_index];
                            tc.AddReply(ParseReply(single_post, board));
                        }
                        il.Add(tc);
                    }
                    return il.ToArray();

                case APIResponse.ErrorType.NotFound:
                    throw new Exception("404");

                case APIResponse.ErrorType.Other:
                    throw new Exception(response.Data);

                default:
                    return null;
            }
        }*/

        public ThreadContainer GetThreadData(string board, int id)
        {
            APIResponse response = LoadAPI("http://a.4cdn.org/#/res/$.json".Replace("#", board).Replace("$", id.ToString()));

            switch (response.Error)
            {
                case APIResponse.ErrorType.NoError:
                    ThreadContainer tc = null;
                    Dictionary<string, object> list = (Dictionary<string, object>)Newtonsoft.Json.JsonConvert.DeserializeObject(response.Data, typeof(Dictionary<string, object>));

                    if (list.ContainsKey("posts"))
                    {
                        Newtonsoft.Json.Linq.JContainer data = (Newtonsoft.Json.Linq.JContainer)list["posts"];
                        tc = new ThreadContainer(ParseThread(data[0], board));

                        for (int index = 1; index < data.Count; index++)
                        {
                            tc.AddReply(ParseReply(data[index], board));
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

        private static Thread ParseThread(Newtonsoft.Json.Linq.JToken data, string board)
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
                switch (data["capcode"].ToString())
                {
                    /*none, mod, admin, admin_highlight, developer*/
                    case "admin":
                    case "admin_highlight":
                        t.Capcode = GenericPost.CapcodeEnum.Admin;
                        break;
                    case "developer":
                        t.Capcode = GenericPost.CapcodeEnum.Developer;
                        break;
                    case "mod":
                        t.Capcode = GenericPost.CapcodeEnum.Mod;
                        break;
                    default:
                        break;
                }
            }

            if (data["sticky"] != null)
            {
                t.IsSticky = (Convert.ToInt32(data["sticky"]) == 1);
            }

            if (data["closed"] != null)
            {
                t.IsClosed = (Convert.ToInt32(data["closed"]) == 1);
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

            t.image_replies = Convert.ToInt32(data["images"]); ;

            t.ID = Convert.ToInt32(data["no"]); ;

            t.text_replies = Convert.ToInt32(data["replies"]);
            t.Time = Common.ParseUTC_Stamp(Convert.ToInt32(data["time"]));


            return t;
        }

        private static PostFile ParseFile(Newtonsoft.Json.Linq.JToken data, string board)
        {
            if (data["filename"] != null)
            {
                PostFile pf = new PostFile();
                pf.filename = HttpUtility.HtmlDecode(data["filename"].ToString());
                pf.ext = data["ext"].ToString().Replace(".", "");
                pf.height = Convert.ToInt32(data["h"]);
                pf.width = Convert.ToInt32(data["w"]);
                pf.thumbW = Convert.ToInt32(data["tn_w"]);
                pf.thumbH = Convert.ToInt32(data["tn_h"]);
                pf.owner = Convert.ToInt32(data["no"]);
                pf.thumbnail_tim = data["tim"].ToString();
                pf.board = board;
                pf.hash = data["md5"].ToString();
                pf.size = Convert.ToInt32(data["fsize"]);
                if (data["spoiler"] != null)
                {
                    pf.IsSpoiler = (Convert.ToInt32(data["spoiler"]) == 1);
                }
                return pf;
            }
            else
            {
                return null;
            }
        }

        private static GenericPost ParseReply(Newtonsoft.Json.Linq.JToken data, string board)
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

            t.File = ParseFile(data, board);

            t.ID = Convert.ToInt32(data["no"]); ;

            t.Time = Common.ParseUTC_Stamp(Convert.ToInt32(data["time"]));

            return t;
        }

        private static CatalogItem ParseJToken_Catalog(Newtonsoft.Json.Linq.JToken thread, int pagenumber, string board)
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
                pf.owner = ci.ID;
                pf.thumbnail_tim = thread["tim"].ToString();
                pf.board = board;

                pf.hash = thread["md5"].ToString();
                pf.size = Convert.ToInt32(thread["fsize"]);

                ci.File = pf;
            }

            if (thread["last_replies"] != null)
            {
                Newtonsoft.Json.Linq.JContainer li = (Newtonsoft.Json.Linq.JContainer)thread["last_replies"];

                List<GenericPost> repl = new List<GenericPost>();

                foreach (Newtonsoft.Json.Linq.JObject j in li)
                {
                    repl.Add(ParseReply(j, board)); // HACK: parent must not be null.
                }

                ci.trails = repl.ToArray();
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

        #endregion

        private static readonly DateTime old_date = DateTime.Now.Subtract(new TimeSpan(365, 0, 0, 0));

        private APIResponse LoadAPI(string url)
        {
            string hash = Common.MD5(url);

            string file_path = Path.Combine(_cache_dir, hash); // contain the last fetch date
            string file_path_data = Path.Combine(_cache_dir, hash + "_data");

            DateTime d = old_date;

            APIResponse result = null;

            if (File.Exists(file_path))
            {
                d = parse_datetime(File.ReadAllText(file_path));
            }

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(url);

            wr.IfModifiedSince = d;

            wr.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:26.0) Gecko/20100101 Firefox/26.0";

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
                            ChanArchiver.NetworkUsageCounter.ApiConsumed += iByteSize;
                        }
                        data = MemIo.ToArray();
                    }
                }

                string text = System.Text.Encoding.UTF8.GetString(data.ToArray());

                string lm = wbr.Headers["Last-Modified"];

                DateTime lmm = DateTime.Parse(lm);

                File.WriteAllText(file_path, datetime_tostring(lmm));
                File.WriteAllText(file_path_data, text);

                result = new APIResponse(text, APIResponse.ErrorType.NoError);

            }
            catch (WebException wex)
            {
                HttpWebResponse httpResponse = wex.Response as HttpWebResponse;
                if (httpResponse != null)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.NotModified)
                    {
                        if (File.Exists(file_path_data))
                        {
                            result = new APIResponse(File.ReadAllText(file_path_data), APIResponse.ErrorType.NoError);
                        }
                        else
                        {
                            delete_file(file_path);
                            delete_file(file_path_data);
                            return LoadAPI(url); //retry fetch
                            //throw new Exception("Reference to a cached file was not found");
                        }
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        result = new APIResponse(null, APIResponse.ErrorType.NotFound);
                        //delete api files since resource has 404'ed
                        delete_file(file_path);
                        delete_file(file_path_data);
                    }
                    else
                    {
                        result = new APIResponse(wex.Message, APIResponse.ErrorType.Other);
                        throw wex;
                    }
                }
                else
                {
                    result = new APIResponse(wex.Message, APIResponse.ErrorType.Other);
                    throw wex;
                }
            }

            if (wbr != null)
            {
                wbr.Close();
            }

            return result;
        }

        private static void delete_file(string s)
        {
            if (File.Exists(s)) { File.Delete(s); }
        }

        private static DateTime parse_datetime(string s)
        {
            return XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.Local);
        }

        private static string datetime_tostring(DateTime s)
        {
            return XmlConvert.ToString(s, XmlDateTimeSerializationMode.Local);
        }

        private static void check_dir(string path)
        {
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
        }
    }
}
