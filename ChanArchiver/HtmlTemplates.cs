using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver
{
    public static class HtmlTemplates
    {
        private const string commons_scripts = "commons-scripts.html";
        private const string commons_sidebar = "commons-sidebar.html";
        private const string commons_headtags = "commons-headtags.html";

        private const string overview_page = "overview-page.html";
        private const string file_queue_page = "file-queue-page.html";
        private const string watch_jobs_page = "watch-jobs-page.html";
        private const string monitored_boards_page = "monitored-boards-page.html";
        private const string thread_filters_page = "thread-filters-page.html";


        public static string CommonScriptsTemplate { get; private set; }
        public static string CommonSidebarTemplate { get; private set; }
        public static string CommonHeadTagsTemplate { get; private set; }

        public static string OverviewPageTemplate { get; private set; }
        public static string FileQueuePageTemplate { get; private set; }
        public static string WatchJobsPageTemplate { get; private set; }
        public static string MonitoredBoardsPageTemplate { get; private set; }
        public static string ThreadFiltersPageTemplate { get; private set; }


        public static void Init()
        {
            Dictionary<string, string> mappings = new Dictionary<string, string>() 
            {
                {overview_page , Properties.Resources.dashboard_page},
                {file_queue_page, Properties.Resources.filequeue_page},
                {commons_scripts, Properties.Resources.commons_script},
                {commons_sidebar, Properties.Resources.commons_sidebar},
                {commons_headtags, Properties.Resources.commons_headtags},
                {watch_jobs_page, Properties.Resources.wjobs_page},
                {monitored_boards_page, Properties.Resources.boards_page},
                {thread_filters_page, Properties.Resources.filters_page}
            };

            foreach (var k in mappings)
            {
                string target_file = Path.Combine(Program.html_templates_dir, k.Key);

                if (!File.Exists(target_file))
                {
                    File.WriteAllText(target_file, k.Value);
                }
            }

            Reload();
        }

        public static void Reload()
        {
            OverviewPageTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, overview_page));
            CommonScriptsTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, commons_scripts));
            CommonSidebarTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, commons_sidebar));
            CommonHeadTagsTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, commons_headtags));
            FileQueuePageTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, file_queue_page));
            WatchJobsPageTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, watch_jobs_page));
            MonitoredBoardsPageTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, monitored_boards_page));
            ThreadFiltersPageTemplate = File.ReadAllText(Path.Combine(Program.html_templates_dir, thread_filters_page));
        }
    }
}
