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

      
        public override string ToString()
        {

            StringBuilder image_template = new StringBuilder(Properties.Resources.image_file);

            image_template.Replace("{file:id}", this.PostID.ToString());

            image_template.Replace("{file:link}", string.Format("/file/{0}", this.Hash + "." + this.FileName.Split('.').Last()));

            image_template.Replace("{file:thumblink}", string.Format("/thumb/{0}", this.Hash + ".jpg"));

            image_template.Replace("{file:name}", this.FileName);
            image_template.Replace("{file:size}", format_size_string(this.Size));

            image_template.Replace("{file:dimensions}",string.Format("{0}x{1}", this.Width , this.Height));

         

            return image_template.ToString();
        }

        public static string format_size_string(double size)
        {
            double KB = 1024;
            double MB = 1048576;
            double GB = 1073741824;
            if (size < KB)
            {
                return size.ToString() + " B";
            }
            else if (size > KB & size < MB)
            {
                return Math.Round(size / KB, 2).ToString() + " KB";
            }
            else if (size > MB & size < GB)
            {
                return Math.Round(size / MB, 2).ToString() + " MB";
            }
            else if (size > GB)
            {
                return Math.Round(size / GB, 2).ToString() + " GB";
            }
            else
            {
                return Convert.ToString(size);
            }
        }
    }

}
