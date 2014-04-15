using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AniWrap.DataTypes
{
    public class CatalogItem : GenericPost
    {
        public int image_replies { get; set; }
        public int text_replies { get; set; }
        public int page_number { get; set; }
        public int TotalReplies { get { return image_replies + text_replies; } }
        public GenericPost[] trails { get; set; }
        public bool IsClosed { get; set; }
        public bool IsSticky { get; set; }
        public int BumpLimit { get; set; }
        public int ImageLimit { get; set; }
    }
}
