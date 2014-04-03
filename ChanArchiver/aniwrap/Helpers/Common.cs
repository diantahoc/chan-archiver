using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

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
    }
}
