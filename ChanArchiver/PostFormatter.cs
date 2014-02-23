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

        public override string ToString()
        {
            StringBuilder template = null;

            switch (this.Type)
            {
                case PostType.OP:
                    template = new StringBuilder(Properties.Resources.op_post);

                    template.Replace("{op:sticky}", this.IsSticky ? "/img/sticky.png" : "");
                    template.Replace("{op:locked}", this.IsLocked ? "/img/locked.png" : "");

                    break;
                case PostType.Reply:
                    template = new StringBuilder(Properties.Resources.reply_post);
                    break;
                default:
                    return "";
            }

            template.Replace("{wpost:id}", this.PostID.ToString());

            //Post subject
            if (string.IsNullOrEmpty(this.Subject))
            {
                template.Replace("{wpost:subject}", "");
            }
            else
            {
                template.Replace("{wpost:subject}", string.Format("<span class='subject'>{0}</span>", this.Subject));
            }

            //Poster ID.
            if (string.IsNullOrEmpty(this.PosterID))
            {
                template.Replace("{wpost:posterId}", "");
            }
            else
            {
                template.Replace("{wpost:posterId}", string.Format("(<span class='posterid'>{0}</span>)", this.PosterID));
            }

            //Post tripcode
            if (string.IsNullOrEmpty(this.Trip))
            {
                template.Replace("{wpost:trip}", "");
            }
            else
            {
                template.Replace("{wpost:trip}", string.Format("<span class='trip'>{0}</span>", this.Trip));
            }

            if (string.IsNullOrEmpty(this.Email))
            {
                template.Replace("{wpost:name}", this.Name);
            }
            else
            {
                template.Replace("{wpost:name}", string.Format("<a href=\"mailto:{0}\">{1}</a>", this.Email, this.Name));
            }

            template.Replace("{wpost:time}", System.Xml.XmlConvert.ToString(this.Time, "yyyy-MM-dd HH:mm:ss"));

            if (this.MyFile == null)
            {
                template.Replace("{wpost:file}", "");

            }
            else
            {
                template.Replace("{wpost:file}", MyFile.ToString());
            }

            if (string.IsNullOrEmpty(this.Comment))
            {
                template.Replace("{wpost:comment}", "");
            }
            else { template.Replace("{wpost:comment}", string.Format("<blockquote class=\"postMessage\">{0}</blockquote>", this.Comment)); }

            return template.ToString();
        }
    }
}
