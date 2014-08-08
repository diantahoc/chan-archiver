using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class SettingsPageHandler : HttpServer.HttpModules.HttpModule
    {

        private const string Checked = "checked=\"\"";
        private const string Selected = "selected=\"selected\"";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command == "/settings")
            {

                StringBuilder page = new StringBuilder(Properties.Resources.settings_page);

                //-------------------- General Settings --------------------------

                if (Settings.AutoStartManuallyAddedThreads)
                {
                    page.Replace("{gs0c}", Checked);
                }
                else
                {
                    page.Replace("{gs0c}", "");
                }

                if (Settings.ThumbnailOnly)
                {
                    page.Replace("{gs1c}", Checked);
                }
                else
                {
                    page.Replace("{gs1c}", "");
                }

                if (Settings.EnableFileStats)
                {
                    page.Replace("{gs2c}", Checked);
                }
                else
                {
                    page.Replace("{gs2c}", "");
                }

                //-------------------- FFMPEG Settings --------------------------

                page.Replace("{ffpath}", Program.ffmpeg_path);


                if (Settings.ConvertGifsToWebm)
                {
                    page.Replace("{ff0c}", Checked);
                }
                else
                {
                    page.Replace("{ff0c}", "");
                }

                if (Settings.ConvertWebmToMp4)
                {
                    page.Replace("{ff1c}", Checked);
                }
                else
                {
                    page.Replace("{ff1c}", "");
                }

                if (Settings.Convert_Webmgif_To_Target)
                {
                    page.Replace("{ff2c}", Checked);
                }
                else
                {
                    page.Replace("{ff2c}", "");
                }


                if (Settings.Convert_Webmgif_Target == Settings.X_Target.GIF)
                {
                    page.Replace("{ff2s1o1c}", Selected);
                    page.Replace("{ff2s1o2c}", "");
                }
                else
                {
                    page.Replace("{ff2s1o1c}", "");
                    page.Replace("{ff2s1o2c}", Selected);
                }

                if (Settings.Convert_Webmgif_only_devices)
                {
                    page.Replace("{ff2s2o1c}", Selected);
                    page.Replace("{ff2s2o2c}", "");
                }
                else
                {
                    page.Replace("{ff2s2o1c}", "");
                    page.Replace("{ff2s2o2c}", Selected);
                }

                //-------------------- File Queue Settings --------------------------

                if (Settings.ListThumbsInQueue)
                {
                    page.Replace("{fq0c}", Checked);
                }
                else
                {
                    page.Replace("{fq0c}", "");
                }


                /*
                if (Settings.PrioritizeBumpLimit)
                {
                    page.Replace("{fq1c}", Checked);
                }
                else
                {
                    page.Replace("{fq1c}", "");
                }

                switch (Settings.FilePrioritizeMode)
                {
                    case Settings.FilePrioritizeModeEnum.BoardSpeed:
                        page.Replace("{fq2s1o1c}", "");
                        page.Replace("{fq2s1o2c}", "");
                        page.Replace("{fq2s1o3c}", Selected);
                        break;
                    case Settings.FilePrioritizeModeEnum.LargerFirst:
                        page.Replace("{fq2s1o1c}", "");
                        page.Replace("{fq2s1o2c}", Selected);
                        page.Replace("{fq2s1o3c}", "");
                        break;
                    case Settings.FilePrioritizeModeEnum.SmallerFirst:
                        page.Replace("{fq2s1o1c}", Selected);
                        page.Replace("{fq2s1o2c}", "");
                        page.Replace("{fq2s1o3c}", "");
                        break;
                    default:
                        break;
                }
                 */

                if (Settings.AutoRemoveCompleteFiles)
                {
                    page.Replace("{fq3c}", Checked);
                }
                else
                {
                    page.Replace("{fq3c}", "");
                }

                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";
                byte[] data = Encoding.UTF8.GetBytes(page.ToString());
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            if (command.StartsWith("/settings/"))
            {
                // -------------- General Settings ------------
                Settings.AutoStartManuallyAddedThreads = request.QueryString["gs0"].Value == "1";
                Settings.ThumbnailOnly = request.QueryString["gs1"].Value == "1";
                Settings.EnableFileStats = request.QueryString["gs2"].Value == "1";

                if (Settings.EnableFileStats) { FileSystemStats.Init(); }


                // -------------- FFMPEG Settings ------------

                Settings.ConvertGifsToWebm = request.QueryString["ff0"].Value == "1";

                Settings.ConvertWebmToMp4 = request.QueryString["ff1"].Value == "1";
                Settings.Convert_Webmgif_To_Target = request.QueryString["ff2"].Value == "1";

                if (request.QueryString["ff2s1"].Value == "gif")
                {
                    Settings.Convert_Webmgif_Target = Settings.X_Target.GIF;
                }
                else
                {
                    Settings.Convert_Webmgif_Target = Settings.X_Target.MP4;
                }


                Settings.Convert_Webmgif_only_devices = request.QueryString["ff2s2"].Value == "ff2s2o1";


                // -------------- File Queue Settings ------------


                Settings.ListThumbsInQueue = request.QueryString["fq0"].Value == "1";
                /*
                Settings.PrioritizeBumpLimit = request.QueryString["fq1"].Value == "1";

                switch (request.QueryString["fq2s1"].Value)
                {
                    case "fq2s1o1":
                        Settings.FilePrioritizeMode = Settings.FilePrioritizeModeEnum.SmallerFirst;
                        break;
                    case "fq2s1o2":
                        Settings.FilePrioritizeMode = Settings.FilePrioritizeModeEnum.LargerFirst;
                        break;
                    case "fq2s1o3":
                        Settings.FilePrioritizeMode = Settings.FilePrioritizeModeEnum.BoardSpeed;
                        break;
                    default:
                        Settings.FilePrioritizeMode = Settings.FilePrioritizeModeEnum.SmallerFirst;
                        break;
                }*/

                Settings.AutoRemoveCompleteFiles = request.QueryString["fq3"].Value == "1";

                Settings.Save();
                response.Redirect("/settings");
                return true;
            }


            return false;
        }
    }
}
