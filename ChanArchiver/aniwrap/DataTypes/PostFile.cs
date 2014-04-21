using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AniWrap.DataTypes
{
    public class PostFile
    {
        public string filename { get; set; }
        public string hash { get; set; }
        public string ext { get; set; }
        public int thumbH { get; set; }
        public int thumbW { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public int size { get; set; }
        public string thumbnail_tim { get; set; }
        public bool IsSpoiler { get; set; }
        public string board { get; set; }

        public GenericPost owner { get; set; }


        public static string NoFile = "NOFILE";

        #region Properties

        public string ThumbLink
        {
            get
            {
                if (board == "f") { return NoFile; }
                return string.Format(Common.thumbLink, this.board, this.thumbnail_tim);
            }
        }

        public string FullImageLink
        {
            get
            {
                if (board == "f")
                {
                    return string.Format(Common.imageLink, this.board, this.filename, this.ext);
                }
                else 
                {
                    return string.Format(Common.imageLink, this.board, this.thumbnail_tim, this.ext);
                }
            }
        }
        #endregion
    }
}
