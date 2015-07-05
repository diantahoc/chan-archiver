using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public abstract class PageHandlerBase
         : HttpServer.HttpModules.HttpModule
    {
        protected void IncludeCommonHtml(StringBuilder target)
        {
            target.Replace("{{commons-headtags}}", GenerateHeadTagsHtml());
            target.Replace("{{commons-sidebar}}", GenerateSidebarHtml());
            target.Replace("{{commons-scripts}}", HtmlTemplates.CommonScriptsTemplate);
        }

        private static string[] sidebar_active_placeholders = new string[] 
        {
            "{{overview-active}}",
            "{{filequeue-active}}",
            "{{watchjobs-active}}",
            "{{monitoredboards-active}}",
            "{{threadfilters-active}}",
            "{{bannedfiles-active}}",
            "{{settings-active}}"
        };

        private string GenerateSidebarHtml()
        {
            int active_index = (int)this.GetPageType();

            if (active_index != -1)
            {
                StringBuilder sidebar = new StringBuilder(HtmlTemplates.CommonSidebarTemplate);

                for (int i = 0; i < sidebar_active_placeholders.Length; i++)
                {
                    if (i == active_index)
                    {
                        sidebar.Replace(sidebar_active_placeholders[i], "active");
                    }
                    else
                    {
                        sidebar.Replace(sidebar_active_placeholders[i], "");
                    }
                }

                return sidebar.ToString();
            }
            else
            {
                return HtmlTemplates.CommonSidebarTemplate;
            }
        }

        private string GenerateHeadTagsHtml() 
        {
            return HtmlTemplates.CommonHeadTagsTemplate.Replace("{{page-title}}", GetPageTitle());
        }

        public enum PageType
            : int
        {
            Overview = 0,
            FileQueue = 1,
            WatchJobs = 2,
            MonitoredBoards = 3,
            ThreadFilters = 4,
            BannedFiles = 5,
            Settings = 6,
            NotSpecified = -1
        }

        public abstract PageType GetPageType();

        public virtual string GetPageTitle()
        {
            return "";
        }

        protected void WriteFinalHtmlResponse(HttpServer.IHttpResponse response, string html)
        {
            byte[] data = Encoding.UTF8.GetBytes(html);
            response.Status = System.Net.HttpStatusCode.OK;
            response.ContentType = ServerConstants.HtmlContentType;
            response.ContentLength = data.Length;
            response.SendHeaders();
            response.SendBody(data);
        }
    }
}
