using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver
{
    public static class Settings
    {

        public static bool AutoStartManuallyAddedThreads { get; set; }

        public static bool ThumbnailOnly { get; set; }

        public static bool EnableFileStats { get; set; }

        public static bool ConvertGifsToWebm { get; set; }

        public static bool ConvertWebmToMp4 { get; set; }

        public static bool RemoveThreadsWhenTheyEnterArchivedState { get; set; }

        /// <summary>
        /// X can be MP4 or GIF
        /// </summary>
        public static bool Convert_Webmgif_To_Target { get; set; }

        public static X_Target Convert_Webmgif_Target { get; set; }

        public enum X_Target { MP4, GIF }

        public static bool Convert_Webmgif_only_devices { get; set; }

        public static bool ListThumbsInQueue { get; set; }

        public enum FilePrioritizeModeEnum { SmallerFirst, LargerFirst, BoardSpeed }

        public static FilePrioritizeModeEnum FilePrioritizeMode { get; set; }

        public static bool PrioritizeBumpLimit { get; set; }

        public static bool ConvertPNGImageWithNoTransparencyToJPG { get; set; }

        public static bool AutoRemoveCompleteFiles { get; set; }

        public static bool UseHttps { get; set; }

        public static void Save()
        {
            Dictionary<string, object> data = new Dictionary<string, object>(11);

            data.Add("AutoStartManuallyAddedThreads", AutoStartManuallyAddedThreads);

            data.Add("ThumbnailOnly", ThumbnailOnly);

            data.Add("EnableFileStats", EnableFileStats);

            data.Add("ConvertGifsToWebm", ConvertGifsToWebm);

            data.Add("ConvertWebmToMp4", ConvertWebmToMp4);

            data.Add("Convert_Webmgif_To_Target", Convert_Webmgif_To_Target);

            data.Add("Convert_Webmgif_Target", Convert.ToInt32(Convert_Webmgif_Target));

            data.Add("Convert_Webmgif_only_devices", Convert_Webmgif_only_devices);

            data.Add("ListThumbsInQueue", ListThumbsInQueue);

            data.Add("FilePrioritizeMode", Convert.ToInt32(FilePrioritizeMode));

            data.Add("PrioritizeBumpLimit", PrioritizeBumpLimit);

            data.Add("ConvertPNGImageWithNoTransparencyToJPG", ConvertPNGImageWithNoTransparencyToJPG);

            data.Add("AutoRemoveCompleteFiles", AutoRemoveCompleteFiles);

            data.Add("UseHttps", UseHttps);

            string _data = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(SettingsSaveFile, _data);
        }

        public static string SettingsSaveFile { get { return System.IO.Path.Combine(Program.program_dir, "settings.json"); } }

        public static void Load()
        {
            if (System.IO.File.Exists(SettingsSaveFile))
            {
                Dictionary<string, object> t = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(System.IO.File.ReadAllText(SettingsSaveFile));
                #region data loading

                if (t.ContainsKey("AutoStartManuallyAddedThreads"))
                {
                    AutoStartManuallyAddedThreads = Convert.ToBoolean(t["AutoStartManuallyAddedThreads"]);
                }

                if (t.ContainsKey("ThumbnailOnly"))
                {
                    ThumbnailOnly = Convert.ToBoolean(t["ThumbnailOnly"]);
                }

                if (t.ContainsKey("EnableFileStats"))
                {
                    EnableFileStats = Convert.ToBoolean(t["EnableFileStats"]);
                }

                if (t.ContainsKey("ConvertGifsToWebm"))
                {
                    ConvertGifsToWebm = Convert.ToBoolean(t["ConvertGifsToWebm"]);
                }

                if (t.ContainsKey("ConvertWebmToMp4"))
                {
                    ConvertWebmToMp4 = Convert.ToBoolean(t["ConvertWebmToMp4"]);
                }

                if (t.ContainsKey("Convert_Webmgif_To_Target"))
                {
                    Convert_Webmgif_To_Target = Convert.ToBoolean(t["Convert_Webmgif_To_Target"]);
                }

                if (t.ContainsKey("Convert_Webmgif_Target"))
                {
                    Convert_Webmgif_Target = (X_Target)Convert.ToInt32(t["Convert_Webmgif_Target"]);
                }

                if (t.ContainsKey("Convert_Webmgif_only_devices"))
                {
                    Convert_Webmgif_only_devices = Convert.ToBoolean(t["Convert_Webmgif_only_devices"]);
                }

                if (t.ContainsKey("ListThumbsInQueue"))
                {
                    ListThumbsInQueue = Convert.ToBoolean(t["ListThumbsInQueue"]);
                }

                if (t.ContainsKey("FilePrioritizeMode"))
                {
                    FilePrioritizeMode = (FilePrioritizeModeEnum)Convert.ToInt32(t["FilePrioritizeMode"]);
                }

                if (t.ContainsKey("PrioritizeBumpLimit"))
                {
                    PrioritizeBumpLimit = Convert.ToBoolean(t["PrioritizeBumpLimit"]);
                }

                if (t.ContainsKey("AutoRemoveCompleteFiles"))
                {
                    AutoRemoveCompleteFiles = Convert.ToBoolean(t["AutoRemoveCompleteFiles"]);
                }

                if (t.ContainsKey("UseHttps"))
                {
                    UseHttps = Convert.ToBoolean(t["UseHttps"]);
                }


                if (t.ContainsKey("RemoveThreadsWhenTheyEnterArchivedState"))
                {
                    RemoveThreadsWhenTheyEnterArchivedState = Convert.ToBoolean(t["RemoveThreadsWhenTheyEnterArchivedState"]);
                }

                #endregion
            }
            else
            {
                //load default settings and save
                AutoStartManuallyAddedThreads = true; ThumbnailOnly = false; EnableFileStats = false; ConvertGifsToWebm = false; ConvertWebmToMp4 = true;
                Convert_Webmgif_To_Target = true; Convert_Webmgif_Target = X_Target.GIF; Convert_Webmgif_only_devices = false;
                ListThumbsInQueue = false; FilePrioritizeMode = FilePrioritizeModeEnum.SmallerFirst; PrioritizeBumpLimit = false;
                ConvertPNGImageWithNoTransparencyToJPG = false; AutoRemoveCompleteFiles = true; UseHttps = true; RemoveThreadsWhenTheyEnterArchivedState = true;
                Save();
            }
        }

    }

}
