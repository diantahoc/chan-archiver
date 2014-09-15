using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ChanArchiver
{
    public static class Wordfilter
    {
        static Wordfilter()
        {
            Load();
        }

        private static string SavePath
        {
            get { return Path.Combine(Program.program_dir, "word-filters.json"); }
        }

        private static List<string> words = new List<string>();

        private static Regex matcher = null;

        public static string Process(string input)
        {
            if (matcher == null) { return input; }
            return matcher.Replace(input, "");
        }

        public static void Save()
        {
            string _data = JsonConvert.SerializeObject(words, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(SavePath, _data);
        }

        public static void Load()
        {
            if (System.IO.File.Exists(SavePath))
            {
                var t = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(SavePath));

                foreach (string s in t)
                {
                    Add(s, false);
                }

                Rebuild();
            }
        }

        private static void Rebuild()
        {
            if (words.Count == 0) { matcher = null; return; }
            StringBuilder a = new StringBuilder();

            for (int i = 0; i < words.Count; i++)
            {
                a.Append(words[i]);
                if (i < words.Count - 1)
                {
                    a.Append('|');
                }

            }
            matcher = new Regex(a.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public static void Add(string s, bool rebuild = true)
        {
            if (!words.Contains(s))
            {
                words.Add(s);
                if (rebuild) { Rebuild(); }
                Save();
            }
        }

        public static void Remove(string s)
        {
            if (words.Contains(s)) { words.Remove(s); Rebuild(); Save(); }
        }

    }
}
