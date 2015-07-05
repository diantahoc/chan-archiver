using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    /// <summary>
    /// A class that holds a list of Url parameters used in various ChanArchiver
    /// HTTP handlers. This is to provide consistancy
    /// </summary>
    public static class UrlParameters
    {
        public const string Board = "board";
        public const string IsEnabled = "isenabled";
        public const string RedirectUrl = "redir";
        public const string DayNumber = "daynumber";
        public const string MonthNumber = "monthnumber";
        public const string ThreadId = "threadid";
        public const string ThreadNotesText = "threadnotestext";
    }
}
