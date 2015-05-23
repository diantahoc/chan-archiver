using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    public class FileFormatter
    {
        public int PostID { get; set; }
        public string Hash { get; set; }
        public string FileName { get; set; }
        public string ThumbName { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int Size { get; set; }

        public string Extension
        {
            get
            {
                if (string.IsNullOrEmpty(this.FileName))
                {
                    return "";
                }
                else
                {
                    return this.FileName.Split('.').Last();
                }
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool relativepath)
        {
            StringBuilder image_template = new StringBuilder(Properties.Resources.file_template);

            image_template.Replace("{post:id}", this.PostID.ToString());

            string image_url_start = relativepath ? "" : "/";

            image_template.Replace("{file:fulllink}", string.Format("{0}file/{1}.{2}", image_url_start, this.Hash, this.Extension));

            image_template.Replace("{file:thumbsrc}", string.Format("{0}thumb/{1}", image_url_start, this.Hash + ".jpg"));

            image_template.Replace("{file:name}", this.FileName);

            image_template.Replace("{file:4chan-md5}", this.Hash);

            image_template.Replace("{file:size}", Program.format_size_string(this.Size));

            image_template.Replace("{file:ext}", this.Extension);

            image_template.Replace("{file:dimensions}", string.Format("{0}x{1}", this.Width, this.Height));

            return image_template.ToString();
        }

    }

}
