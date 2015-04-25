using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json;
using Jayrock.Json.Conversion;

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

        public static bool EnableAuthentication { get; set; }

        public static string AuthUsername { get; set; }

        public static string AuthPassword { get; set; }

        public static bool AllowGuestAccess { get; set; }

        public static bool SaveBannedFileThumbnail { get; set; }

        public static bool CacheAPIFilesInMemory { get; set; }

        public static void Save()
        {
            Dictionary<string, object> data = new Dictionary<string, object>(20);

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

            data.Add("EnableAuthentication", EnableAuthentication);

            data.Add("AuthUsername", AuthUsername);

            data.Add("AuthPassword", AuthPassword);

            data.Add("AllowGuestAccess", AllowGuestAccess);

            data.Add("SaveBannedFileThumbnail", SaveBannedFileThumbnail);

            data.Add("CacheAPIFilesInMemory", CacheAPIFilesInMemory);

            System.IO.File.WriteAllText(SettingsSaveFile, JsonConvert.ExportToString(data));
        }

        public static string SettingsSaveFile { get { return System.IO.Path.Combine(Program.program_dir, "settings.json"); } }

        public static void Load()
        {
            if (System.IO.File.Exists(SettingsSaveFile))
            {
                JsonObject t = JsonConvert.Import<JsonObject>(System.IO.File.ReadAllText(SettingsSaveFile));

                #region data loading

                foreach (JsonMember member in t)
                {
                    switch (member.Name)
                    {
                        case "AutoStartManuallyAddedThreads":
                            AutoStartManuallyAddedThreads = Convert.ToBoolean(member.Value); continue;

                        case "ThumbnailOnly":
                            ThumbnailOnly = Convert.ToBoolean(member.Value); continue;

                        case "EnableFileStats":
                            EnableFileStats = Convert.ToBoolean(member.Value); continue;

                        case "ConvertGifsToWebm":
                            ConvertGifsToWebm = Convert.ToBoolean(member.Value); continue;

                        case "ConvertWebmToMp4":
                            ConvertWebmToMp4 = Convert.ToBoolean(member.Value); continue;

                        case "Convert_Webmgif_To_Target":
                            Convert_Webmgif_To_Target = Convert.ToBoolean(member.Value); continue;

                        case "Convert_Webmgif_only_devices":
                            Convert_Webmgif_only_devices = Convert.ToBoolean(member.Value); continue;

                        case "ListThumbsInQueue":
                            ListThumbsInQueue = Convert.ToBoolean(member.Value); continue;

                        case "Convert_Webmgif_Target":
                            Convert_Webmgif_Target = (X_Target)Convert.ToInt32(member.Value); continue;

                        case "FilePrioritizeMode":
                            FilePrioritizeMode = (FilePrioritizeModeEnum)Convert.ToInt32(member.Value); continue;

                        case "PrioritizeBumpLimit":
                            PrioritizeBumpLimit = Convert.ToBoolean(member.Value); continue;

                        case "AutoRemoveCompleteFiles":
                            AutoRemoveCompleteFiles = Convert.ToBoolean(member.Value); continue;

                        case "UseHttps":
                            UseHttps = Convert.ToBoolean(member.Value); continue;

                        case "RemoveThreadsWhenTheyEnterArchivedState":
                            RemoveThreadsWhenTheyEnterArchivedState = Convert.ToBoolean(member.Value); continue;

                        case "EnableAuthentication":
                            EnableAuthentication = Convert.ToBoolean(member.Value); continue;

                        case "SaveBannedFileThumbnail":
                            SaveBannedFileThumbnail = Convert.ToBoolean(member.Value); continue;

                        case "AllowGuestAccess":
                            AllowGuestAccess = Convert.ToBoolean(member.Value); continue;

                        case "AuthUsername":
                            AuthUsername = Convert.ToString(member.Value); continue;

                        case "AuthPassword":
                            AuthPassword = Convert.ToString(member.Value); continue;

                        case "CacheAPIFilesInMemory":
                            CacheAPIFilesInMemory = Convert.ToBoolean(member.Value); continue;
                    }
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
                EnableAuthentication = true; AuthPassword = "admin"; AuthUsername = "admin"; AllowGuestAccess = false; SaveBannedFileThumbnail = true;
                CacheAPIFilesInMemory = true;
                Save();
            }
        }

    }

}
