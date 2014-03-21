using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class FileQueuePageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            if (command == "/fq" || command == "/fq/")
            {

                StringBuilder sb = new StringBuilder();

                for (int index = 0; index < Program.queued_files.Keys.Count; index++)
                {
                    try
                    {
                        KeyValuePair<string, FileQueueStateInfo> kvp = Program.queued_files.ElementAt(index);

                        FileQueueStateInfo f = kvp.Value;

                        sb.AppendFormat("<tr id='{0}'>", kvp.Key);

                        sb.AppendFormat("<td>{0}</td>", get_file_status(f.Status));

                        sb.AppendFormat("<td>{0}</td>", f.RetryCount);

                        sb.AppendFormat("<td>{0}</td>", f.Type.ToString());
                        sb.AppendFormat("<td>{0}</td>", f.Hash);

                        sb.AppendFormat("<td>{0}</td>", f.Url);

                        sb.AppendFormat("<td>{0} %</td>", Math.Round(f.Percent(), 2));

                        sb.AppendFormat("<td> <a href=\"/fileinfo/{0}{1}\" class=\"label label-primary\">Info</a> </td>", f.Type == FileQueueStateInfo.FileType.FullFile ? "file" : "thumb", f.Hash);

                        sb.Append("</tr>");

                    }
                    catch (Exception)
                    {
                        if (index >= Program.queued_files.Keys.Count) { break; }
                    }
                }



                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.filequeue_page.Replace("{mff}", Program.file_stp.MaxThreads.ToString()).Replace("{Files}", sb.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            if (command.StartsWith("/fq/json/"))
            {
                if (string.IsNullOrEmpty(request.QueryString["hash"].Value))
                {
                    ThreadServerModule._404(response);
                }
                else
                {
                    bool ifo = !string.IsNullOrWhiteSpace(request.QueryString["ifo"].Value);

                    if (Program.queued_files.ContainsKey(request.QueryString["hash"].Value))
                    {
                        FileQueueStateInfo f = Program.queued_files[request.QueryString["hash"].Value];

                        Dictionary<string, object> dt = new Dictionary<string, object>();
                        if (ifo)
                        {
                            dt.Add("p", f.Percent().ToString());
                            dt.Add("s", string.Format("{0} / {1}", Program.format_size_string(f.Downloaded), Program.format_size_string(f.Length)));
                            dt.Add("c", f.Status == FileQueueStateInfo.DownloadStatus.Complete);
                        }
                        else
                        {
                            dt.Add("Status", get_file_status(f.Status));
                            dt.Add("RetryCount", f.RetryCount);
                            dt.Add("Percent", string.Format("{0} %", Math.Round(f.Percent(), 2)));
                        }
                        response.Status = System.Net.HttpStatusCode.OK;
                        response.ContentType = "application/json";

                        string text = Newtonsoft.Json.JsonConvert.SerializeObject(dt);

                        ThreadServerModule.write_text(text, response);
                    }
                }
                return true;
            }


            return false;
        }

        private string get_file_status(FileQueueStateInfo.DownloadStatus s)
        {
            switch (s)
            {
                case FileQueueStateInfo.DownloadStatus.Downloading:
                    return "<span class=\"label label-info\">Downloading</span>";
                case FileQueueStateInfo.DownloadStatus.Pending:
                    return ("<span class=\"label label-warning\">Pending</span>");
                case FileQueueStateInfo.DownloadStatus.Error:
                    return ("<span class=\"label label-danger\">Error</span>");
                case FileQueueStateInfo.DownloadStatus.Complete:
                    return ("<span class=\"label label-success\">Complete</span>");
                case FileQueueStateInfo.DownloadStatus.Unstarted:
                    return ("<span class=\"label label-default\">Unstarted</span>");
                default:
                    return ("<span class=\"label label-default\">Unkown</span>");
            }
        }

    }


}
