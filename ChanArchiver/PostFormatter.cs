using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    public class PostFormatter
    {
        public enum PostType { Unknown, OP, Reply }

        public PostFormatter()
        {
            this.Type = PostType.Unknown;
        }

        public int PostID { get; set; }

        public string Name { get; set; }
        public string PosterID { get; set; }
        public string Subject { get; set; }
        public string Email { get; set; }
        public string Trip { get; set; }
        public string Comment { get; set; }
        public DateTime Time { get; set; }

        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }

        public PostType Type { get; set; }

        public FileFormatter MyFile { get; set; }

        private int GetTicks(DateTime t)
        {
            try
            {
                return Convert.ToInt32((t - AniWrap.Common.UnixEpoch).TotalSeconds);
            }
            catch
            {
                return 0;
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool isRelativePath)
        {
            StringBuilder template = null;

            string image_url_start = isRelativePath ? "" : "/";

            switch (this.Type)
            {
                case PostType.OP:
                    template = new StringBuilder(Properties.Resources.op_template);

                    template.Replace("{op:sticky}", this.IsSticky ? string.Format("<img src='{0}res/sticky.png' />", image_url_start) : "");
                    template.Replace("{op:locked}", this.IsLocked ? string.Format("<img src='{0}res/locked.png' />", image_url_start) : "");

                    break;
                case PostType.Reply:
                    template = new StringBuilder(Properties.Resources.reply_template);
                    break;
                default:
                    return "";
            }

            template.Replace("{post:id}", this.PostID.ToString());

            //Post subject
            if (string.IsNullOrEmpty(this.Subject))
            {
                template.Replace("{post:subject}", "");
            }
            else
            {
                template.Replace("{post:subject}", this.Subject);
            }

            //Poster ID.
            if (string.IsNullOrEmpty(this.PosterID))
            {
                template.Replace("{post:posterid}", "");
            }
            else
            {
                template.Replace("{post:posterid}", string.Format("<span class=\"posteruid id_{0}\">(ID: <span class=\"hand\" title=\"Highlight posts by this ID\">{0}</span>)</span>", this.PosterID));
            }

            //Post tripcode
            if (string.IsNullOrEmpty(this.Trip))
            {
                template.Replace("{post:trip}", "");
            }
            else
            {
                template.Replace("{post:trip}", string.Format("<span class=\"postertrip\">{0}</span>", this.Trip));
            }

            if (string.IsNullOrEmpty(this.Email))
            {
                template.Replace("{post:nameblock}", string.Format("<span class=\"name\">{0}</span>", this.Name));
            }
            else
            {
                template.Replace("{post:nameblock}", string.Format("<a href=\"mailto:{0}\" class=\"useremail\"><span class=\"name\">{1}</span></a>", this.Email, this.Name));
            }

            template.Replace("{post:datetime}", System.Xml.XmlConvert.ToString(this.Time, "yyyy-MM-dd HH:mm:ss"));

            template.Replace("{post:unixepoch}", GetTicks(this.Time).ToString());

            if (this.MyFile == null)
            {
                template.Replace("{post:file}", "");
            }
            else
            {
                template.Replace("{post:file}", MyFile.ToString(isRelativePath));
            }

            if (string.IsNullOrEmpty(this.Comment))
            {
                template.Replace("{post:comment}", "");
            }
            else { template.Replace("{post:comment}", this.Comment); }

            return template.ToString();
        }
    }
}
