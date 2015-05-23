using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver.HttpServerHandlers
{
    class FileBrowserPageHandler : HttpServer.HttpModules.HttpModule
    {

        Func<string, bool>[] Selectors = new Func<string, bool>[] 
        {
            new Func<string, bool>(x =>  { return x.EndsWith(".webm"); } ),
            new Func<string, bool>(x =>  { return x.EndsWith(".webm") && !x.EndsWith(".gif.webm"); } ),
            new Func<string, bool>(x =>  { return x.EndsWith(".jpg") ;} ),
            new Func<string, bool>(x =>  { return x.EndsWith(".png") ;} ),
            new Func<string, bool>(x =>  { return x.EndsWith(".gif") ;} )
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
                        string filename = System.IO.Path.GetFileName(path);
                        if (has_reached_next_page)
                        {
                            if (counter > limit) { next_value = filename; break; }
                            if (fc(filename))
                            {
                                string file_name_without_extension = System.IO.Path.GetFileNameWithoutExtension(filename);
                                string ext = System.IO.Path.GetExtension(filename);

                                sb.AppendFormat("<a href=\"/file/{0}{1}\"><img src=\"/thumb/{0}.jpg\" /></a><br />",
                                    file_name_without_extension, ext);
                                counter++;
                            }
                        }
                        else
                        {
                            has_reached_next_page = prev_value == filename;
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
