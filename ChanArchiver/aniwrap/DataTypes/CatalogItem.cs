using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AniWrap.DataTypes
{
    public class CatalogItem : GenericPost
    {
        public int image_replies;
        public int text_replies;
        public int page_number;
        public int TotalReplies { get { return image_replies + text_replies; } }
        public GenericPost[] trails;
    }
}
