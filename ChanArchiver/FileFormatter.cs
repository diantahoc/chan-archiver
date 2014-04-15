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

            StringBuilder image_template = new StringBuilder(Properties.Resources.image_file);

            image_template.Replace("{file:id}", this.PostID.ToString());

            image_template.Replace("{file:link}", string.Format("/file/{0}.{1}", this.Hash, this.Extension));

            image_template.Replace("{file:thumblink}", string.Format("/thumb/{0}", this.Hash + ".jpg"));

            image_template.Replace("{file:name}", this.FileName);
            image_template.Replace("{file:size}", Program.format_size_string(this.Size));

            image_template.Replace("{file:dimensions}", string.Format("{0}x{1}", this.Width, this.Height));



            return image_template.ToString();
        }

    }

}
