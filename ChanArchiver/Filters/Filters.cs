using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChanArchiver.Filters
{
    public class CommentFilter : IFilter
    {
        private Regex matcher;

        public CommentFilter(string exp)
        {
            if (!string.IsNullOrEmpty(exp))
            {
                this.FilterText = exp;
                this.matcher = new Regex(exp, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
        }

        public string FilterText { get; private set; }

        public bool Detect(AniWrap.DataTypes.GenericPost post)
        {
            return matcher.IsMatch(post.CommentText);
        }

        public string Notes { get; set; }

        public FilterType Type { get; set; }
    }

    public class SubjectFilter : IFilter
    {
        private Regex matcher;

        public SubjectFilter(string exp)
        {
            if (!string.IsNullOrEmpty(exp))
            {
                this.FilterText = exp;
                this.matcher = new Regex(exp, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
        }

        public string FilterText { get; private set; }

        public bool Detect(AniWrap.DataTypes.GenericPost post)
        {
            return matcher.IsMatch(post.Subject);
        }

        public string Notes { get; set; }

        public FilterType Type { get; set; }
    }

    public class TripFilter : IFilter
    {
        private Regex matcher;

        public TripFilter(string exp)
        {
            if (!string.IsNullOrEmpty(exp))
            {
                this.FilterText = exp;
                this.matcher = new Regex(exp, RegexOptions.Compiled);
            }
        }

        public string FilterText { get; private set; }

        public bool Detect(AniWrap.DataTypes.GenericPost post)
        {
            return matcher.IsMatch(post.Trip);
        }

        public string Notes { get; set; }

        public FilterType Type { get; set; }
    }

    public class NameFilter : IFilter
    {
        private Regex matcher;

        public NameFilter(string exp)
        {
            if (!string.IsNullOrEmpty(exp))
            {
                this.FilterText = exp;
                this.matcher = new Regex(exp, RegexOptions.Compiled);
            }
        }

        public string FilterText { get; private set; }

        public bool Detect(AniWrap.DataTypes.GenericPost post)
        {
            return matcher.IsMatch(post.Name);
        }
        public string Notes { get; set; }

        public FilterType Type { get; set; }
    }

    public class EmailFilter : IFilter
    {
        private Regex matcher;

        public EmailFilter(string exp)
        {
            if (!string.IsNullOrEmpty(exp))
            {
                this.FilterText = exp;
                this.matcher = new Regex(exp, RegexOptions.Compiled);
            }
        }

        public string FilterText { get; private set; }

        public bool Detect(AniWrap.DataTypes.GenericPost post)
        {
            return matcher.IsMatch(post.Email);
        }

        public string Notes { get; set; }

        public FilterType Type { get; set; }
    }

    public class FileNameFilter : IFilter
    {
        private Regex matcher;

        public FileNameFilter(string exp)
        {
            if (!string.IsNullOrEmpty(exp))
            {
                this.FilterText = exp;
                this.matcher = new Regex(exp, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
        }

        public string FilterText { get; private set; }

        public bool Detect(AniWrap.DataTypes.GenericPost post)
        {
            if (post.File != null)
            {
                return matcher.IsMatch(post.File.filename);
            }
            else
            {
                return false;
            }
        }
        public string Notes { get; set; }
        public FilterType Type { get; set; }
    }

    public class FileHashFilter : IFilter
    {
        public FileHashFilter(string exp)
        {
            if (!string.IsNullOrEmpty(exp))
            {
                this.FilterText = exp;
            }
        }

        public string FilterText { get; private set; }

        public bool Detect(AniWrap.DataTypes.GenericPost post)
        {
            if (post.File != null)
            {
                //    4chan hash or md5 hash???????
                return post.File.hash == this.FilterText;
            }
            else
            {
                return false;
            }
        }
        public string Notes { get; set; }
        public FilterType Type { get; set; }
    }

    //public class ImageFilter : IFilter
    //{
    //    private byte[,] redmap;

    //    public ImageFilter(string redmap_exp)
    //    {

    //    }

    //    public string FilterText { get { return "(image data)"; } }
   
    //}

}
