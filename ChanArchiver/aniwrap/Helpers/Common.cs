using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using HtmlAgilityPack;
namespace AniWrap
{
    public static class Common
    {
        /// <summary>
        /// {0}: http_prefix, {1}: board, {2}: tim, {3}: ext
        /// </summary>
        public static readonly string imageLink = "{0}://i.4cdn.org/{1}/{2}.{3}";
        /// <summary>
        /// {0}: http_prefix, {1}:board, {2}: tim
        /// </summary>
        public static readonly string thumbLink = "{0}://t.4cdn.org/{1}/{2}s.jpg";

        public static string HttpPrefix
        {
            get
            {
                if (ChanArchiver.Settings.UseHttps)
                {
                    return "https";
                }
                else
                {
                    return "http";
                }
            }
        }

        public static readonly DateTime UnixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static DateTime ParseUTC_Stamp(int timestamp)
        {
            return UnixEpoch.AddSeconds(timestamp); ;
        }

        public static string MD5(string s)
        {
            using (MD5CryptoServiceProvider md5s = new MD5CryptoServiceProvider())
            {
                return ByteArrayToString(md5s.ComputeHash(System.Text.Encoding.ASCII.GetBytes(s)));
            }
        }

        private static string ByteArrayToString(byte[] arrInput)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte x in arrInput)
            {
                sb.Append(x.ToString("X2"));
            }
            return sb.ToString().ToLower();
        }

        public static string DecodeHTML(string text)
        {
            if (!(String.IsNullOrEmpty(text) || String.IsNullOrWhiteSpace(text)))
            {
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument(); doc.LoadHtml(text);
                return System.Web.HttpUtility.HtmlDecode(Common.GetNodeText(doc.DocumentNode));
            }
            else
            {
                return "";
            }
        }

        private static string GetNodeText(HtmlNode node)
        {
            StringBuilder sb = new StringBuilder();

            if (node.Name == "br")
            {
                sb.AppendLine();
            }
            else if (node.ChildNodes.Count > 0)
            {
                foreach (HtmlNode a in node.ChildNodes)
                {
                    sb.Append(Common.GetNodeText(a));
                }
            }
            else
            {
                return node.InnerText;
            }

            return sb.ToString();
        }
    }
}
