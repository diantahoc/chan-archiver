﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Jayrock;
using Jayrock.Json;
using Jayrock.Json.Conversion;
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
            File.WriteAllText(SavePath, JsonConvert.ExportToString(words));
        }

        public static void Load()
        {
            if (System.IO.File.Exists(SavePath))
            {
                JsonArray t = JsonConvert.Import<JsonArray>(File.ReadAllText(SavePath));

                foreach (object s in t)
                {
                    Add(Convert.ToString(s), false);
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
                a.Append(Regex.Escape(words[i]));
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
