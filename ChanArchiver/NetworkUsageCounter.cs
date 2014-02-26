using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    public static class NetworkUsageCounter
    {

        public static double ApiConsumed { get; set; }
        public static double FileConsumed { get; set; }
        public static double ThumbConsumed { get; set; }


        public static double Total { get { return ApiConsumed + FileConsumed + ThumbConsumed; } }

    }
}
