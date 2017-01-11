using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public sealed class BannedFilesPageHandler
        : PageHandlerBase
    {
        public const string Url = "/bannedfiles";

        const int item_per_page = 15;

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command.StartsWith(Url))
            {
                StringBuilder page = new StringBuilder(HtmlTemplates.BannedFilesPageTemplate);

                IncludeCommonHtml(page);

                StringBuilder items = new StringBuilder();

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
                    items.AppendFormat(@"
                    <tr style='height=55px;'>
                    <td><img src='/thumb/{0}.jpg' style='max-width:75px; max-height:55px' />
                    </td><td>{0}</td><td><a class=""btn btn-default"" href='/action/unbanfile?hash={0}' title='Remove Ban'><i class=""fa fa-trash-o""></i></a></td></tr>", hashes[j]);
                }

                page.Replace("{{items}}", items.ToString());

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

                page.Replace("{{page-numbers}}", page_numbers.ToString());

                WriteFinalHtmlResponse(response, page.ToString());

                return true;
            }

            return false;
        }

        public override PageType GetPageType()
        {
            return PageType.BannedFiles;
        }

        public override string GetPageTitle()
        {
            return "Banned Files";
        }
    }
}
