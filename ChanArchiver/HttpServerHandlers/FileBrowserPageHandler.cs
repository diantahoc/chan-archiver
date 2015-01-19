using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver.HttpServerHandlers
{
    class FileBrowserPageHandler : HttpServer.HttpModules.HttpModule
    {

        Func<FileInfo, bool>[] Selectors = new Func<FileInfo, bool>[] 
        {
            new Func<FileInfo, bool>(x =>  { return x.Name.EndsWith(".webm"); } ),
            new Func<FileInfo, bool>(x =>  { return x.Name.EndsWith(".webm") && !x.Name.EndsWith(".gif.webm"); } ),
            new Func<FileInfo, bool>(x =>  { return x.Name.EndsWith(".jpg") ;} ),
            new Func<FileInfo, bool>(x =>  { return x.Name.EndsWith(".png") ;} ),
            new Func<FileInfo, bool>(x =>  { return x.Name.EndsWith(".gif") ;} )
        };

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString(); ;

            if (command.StartsWith("/filetree"))
            {
                int selector_index = -1;

                string file_type = request.QueryString["ftype"].Value;

                if (!string.IsNullOrEmpty(file_type))
                {
                    Int32.TryParse(file_type, out selector_index);
                }

                int limit = 250;

                string limit_user = request.QueryString["limit"].Value;

                if (!string.IsNullOrEmpty(limit_user))
                {
                    Int32.TryParse(request.QueryString["limit"].Value, out limit);
                }

                StringBuilder sb = new StringBuilder();

                string filter = "blank";

                string prev_value = request.QueryString["prev_value"].Value;

                string next_value = "";

                if (!(selector_index < 0 || selector_index > 4))
                {
                    switch (selector_index)
                    {
                        case 0:
                        case 1:
                            filter = "*.webm"; break;
                        case 2:
                            filter = "*.jpg"; break;
                        case 3:
                            filter = "*.png"; break;
                        case 4:
                            filter = "*.gif"; break;
                        default: break;

                    }

                    var fc = Selectors[selector_index];

                    int counter = 0;

                    bool has_reached_next_page = false;

                    has_reached_next_page = string.IsNullOrEmpty(prev_value);

                    foreach (string path in FileOperations.EnumerateOptimizedDirectory(Program.file_save_dir))
                    {
                        FileInfo fi = new FileInfo(path);
                        if (has_reached_next_page)
                        {
                            if (counter > limit) { next_value = fi.Name; break; }
                            if (fc(fi))
                            {
                                string h = fi.Name.Split('.')[0];
                                string ext = fi.Name.Substring(h.Length);

                                sb.AppendFormat("<a href=\"/file/{0}{1}\"><img src=\"/thumb/{0}.jpg\" /></a><br />", h, ext);
                                counter++;
                            }
                        }
                        else
                        {
                            has_reached_next_page = prev_value == fi.Name;
                        }
                    }
                }


                StringBuilder page = new StringBuilder(Properties.Resources.file_list_page);

                for (int i = 0; i < 4; i++) 
                {
                    if (i == selector_index) 
                    {
                        page.Replace("{o" + i.ToString() + "}", "selected='selected'");
                    }
                    else 
                    {
                        page.Replace("{o" + i.ToString() + "}", "");
                    }
                }

                page//.Replace("{prev}", prev_value)
                  .Replace("{next}", next_value)
                  .Replace("{ftvalue}", filter)
                  .Replace("{lvalue}", limit.ToString())
                  .Replace("{items}", sb.ToString());

                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(page.ToString());
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            return false;
        }
    }
}
