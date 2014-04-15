using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChanArchiver
{
    public static class NetworkUsageCounter
    {
        static NetworkUsageCounter()
        {
            LoadStats();
        }

        private static string NetworkStatsSavePath
        {
            get { return Path.Combine(Program.program_dir, "network-stats.json"); }
        }

        static bool stats_not_loaded = true;

        private static Dictionary<string, double[]> data_history = new Dictionary<string, double[]>();



        public static double ApiConsumedThisHour { get { return get_today_array()[0]; } }
        public static double FileConsumedThisHour { get { return get_today_array()[1]; } }
        public static double ThumbConsumedThisHour { get { return get_today_array()[2]; } }

        public static double TotalThisHour
        {
            get { return ApiConsumedThisHour + FileConsumedThisHour + ThumbConsumedThisHour; }
        }

        public static double TotalConsumedAllTime
        {
            get
            {
                string[] keys = data_history.Keys.ToArray();
                double l = 0;
                foreach (string key in keys)
                {
                    l += (data_history[key][0] + data_history[key][1] + data_history[key][2]);
                }
                return l;
            }
        }


        public static void Add_ApiConsumed(double value)
        {
            if (stats_not_loaded) { LoadStats(); }
            get_today_array()[0] += value;
        }

        public static void Add_FileConsumed(double value)
        {
            if (stats_not_loaded) { LoadStats(); }
            get_today_array()[1] += value;
        }

        public static void Add_ThumbConsumed(double value)
        {
            if (stats_not_loaded) { LoadStats(); }
            get_today_array()[2] += value;
        }

        private static double[] get_today_array()
        {
            string k = string.Format("{0}-{1}-{2}-{3}", DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year, DateTime.Now.Hour);

            if (data_history.ContainsKey(k))
            {
                return data_history[k];
            }
            else
            {
                double[] t = new double[3];
                t[0] = 0;
                t[1] = 0;
                t[2] = 0;
                data_history.Add(k, t);
                return t;
            }
        }

        /// <summary>
        /// Get total consumed for each hour of the day
        /// </summary>
        /// <param name="day"></param>
        /// <returns></returns>
        public static KeyValuePair<int, double>[] GetDayStats(DateTime day)
        {
            string[] keys = data_history.Keys.ToArray();

            List<KeyValuePair<int, double>> il = new List<KeyValuePair<int, double>>();

            string d = string.Format("{0}-{1}-{2}", day.Day, day.Month, day.Year);

            foreach (string key in keys)
            {
                if (key.StartsWith(d))
                {
                    double[] ss = data_history[key];

                    double sum = ss[0] + ss[1] + ss[2];

                    il.Add(new KeyValuePair<int, double>(Convert.ToInt32(key.Replace(d + "-", "")), sum));
                }
            }
            return il.ToArray();
        }


        #region Save & Load

        public static void LoadStats()
        {
            if (File.Exists(NetworkStatsSavePath) && stats_not_loaded)
            {
                string data = File.ReadAllText(NetworkStatsSavePath);
                Dictionary<string, object> t = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                if (t != null)
                {
                    for (int i = 0; i < t.Count; i++)
                    {
                        KeyValuePair<string, object> a = t.ElementAt(i);

                        if (a.Value.GetType() == typeof(JArray))
                        {
                            JArray sdata = (JArray)a.Value;
                            double[] li = new double[3];

                            li[0] = sdata[0].ToObject<double>();
                            li[1] = sdata[1].ToObject<double>();
                            li[2] = sdata[2].ToObject<double>();

                            data_history.Add(a.Key, li);
                        }
                    }
                }
            }
            stats_not_loaded = false;
        }

        public static void SaveStats()
        {
            string d = JsonConvert.SerializeObject(data_history);
            File.WriteAllText(NetworkStatsSavePath, d);
        }

        #endregion
    }
}