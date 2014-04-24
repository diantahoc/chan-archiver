using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.Filters
{
    public interface IFilter
    {
        bool Detect(AniWrap.DataTypes.GenericPost post);
        string FilterText { get; }
        string Notes { get; set; }
    }
}
