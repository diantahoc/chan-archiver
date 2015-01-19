using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class BannedFilesPageHandler : HttpServer.HttpModules.HttpModule
    {
        const int item_per_page = 10;
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command.StartsWith("/bannedfiles"))
            {
                StringBuilder sb = new StringBuilder();

                string[] hashes = Program.get_banned_file_list();

                int item_count = hashes.Length;

                int rem = (item_count % item_per_page);

                int page_count = ((item_count - rem) / item_per_page) + (rem > 0 ? 1 : 0);

                if (page_count <= 0) { page_count = 1; }

                int page_offset = 0;

                Int32.TryParse(request.QueryString["pn"].Value, out page_offset); page_offset = Math.Abs(page_offset);

                int start = page_offset * (item_per_page);

                for (int i = 0, j = start + i; i < item_per_page && (j < item_count); i++, j = start + i)
                {
                    sb.AppendFormat(@"<tr style='height=55px;'><td><img src='/thumb/{0}.jpg' style='max-width:75px; max-height:55px' /></td><td>{0}</td><td><a class=""btn btn-default"" href='/action/unbanfile?hash={0}' title='Remove'><i class=""fa fa-trash-o""></i></a></td></tr>",
                            hashes[j]);
                }

                StringBuilder page_numbers = new StringBuilder();

                for (int i = 0; i < page_count; i++)
                {
                    if (i == page_offset)
                    {
                        page_numbers.AppendFormat("<li class=\"active\"><a href=\"?pn={0}\">{1}</a></li>", i, i + 1);
                    }
                    else
                    {
                        page_numbers.AppendFormat("<li><a href=\"?pn={0}\">{1}</a></li>", i, i + 1);
                    }
                }

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(
                    Properties.Resources.banned_files_page.Replace("{pagenumbers}", page_numbers.ToString())
                    .Replace("{Items}", sb.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;

            }

            return false;
        }
    }
}
