using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
   public class LogEntry
    {
       /// <summary>
       /// Title is used to identifie the sender.
       /// Such as : "/g/" or "/g/ - 124" or "-"
       /// </summary>
       /// 
       public string Title { get; set; }
       public string Sender { get; set; }

       public string Message { get; set; }
       public DateTime Time { get; private set; }

       public enum LogLevel { Info, Warning, Success, Fail }
       public LogLevel Level { get; set; }

       public LogEntry() 
       {
           this.Time = DateTime.Now;
       }

    }
}
