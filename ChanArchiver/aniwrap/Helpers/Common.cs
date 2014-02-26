using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AniWrap
{
    public static class Common
    {
        public static string imageLink = @"http://i.4cdn.org/#/src/$";
        public static string thumbLink = @"http://t.4cdn.org/#/thumb/$s.jpg";

        private static readonly DateTime UnixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static DateTime ParseUTC_Stamp(int timestamp)
        {
            return UnixEpoch.AddSeconds(timestamp); ;
        }

        private static System.Security.Cryptography.MD5CryptoServiceProvider md5s = new System.Security.Cryptography.MD5CryptoServiceProvider();

        public static string MD5(string s)
        {
            return ByteArrayToString(md5s.ComputeHash(System.Text.Encoding.ASCII.GetBytes(s))); ;
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
    }
}
