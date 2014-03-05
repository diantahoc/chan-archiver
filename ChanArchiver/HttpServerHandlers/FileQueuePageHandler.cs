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
                        FileQueueStateInfo f = Program.queued_files.ElementAt(index).Value;

                        sb.Append("<tr>");

                        switch (f.Status)
                        {
                            case FileQueueStateInfo.DownloadStatus.Downloading:
                                sb.Append("<td><span class=\"label label-info\">Downloading</span></td>");
                                break;
                            case FileQueueStateInfo.DownloadStatus.Pending:
                                sb.Append("<td><span class=\"label label-warning\">Pending</span></td>");
                                break;
                            case FileQueueStateInfo.DownloadStatus.Error:
                                sb.Append("<td><span class=\"label label-danger\">Error</span></td>");
                                break;
                            case FileQueueStateInfo.DownloadStatus.Complete:
                                sb.Append("<td><span class=\"label label-success\">Complete</span></td>");
                                break;
                            default:
                                sb.Append("<td><span class=\"label label-default\">Unkown</span></td>");
                                break;
                        }

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

                byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.filequeue_page.Replace("{Files}", sb.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }


            return false;
        }

    }
}
