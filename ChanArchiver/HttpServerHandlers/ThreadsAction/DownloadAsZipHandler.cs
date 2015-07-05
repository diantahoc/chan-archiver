using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver.HttpServerHandlers.ThreadsAction
{
    public class DownloadAsZipHandler
        : HttpServer.HttpModules.HttpModule
    {
        public const string Url = "/action/zipthread/";

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath.StartsWith(Url))
            {

                string board = request.QueryString[UrlParameters.Board].Value;
                string threadIdStr = request.QueryString[UrlParameters.ThreadId].Value;
                int threadId = -1;
                int.TryParse(threadIdStr, out threadId);

                if (!Program.IsBoardLetterValid(board))
                {
                    ThreadServerModule.write_text("Invalid board letter", response);
                    return true;
                }

                if (threadId <= 0)
                {
                    ThreadServerModule.write_text("Invalid thread id", response);
                    return true;
                }

                PostFormatter[] thread_data = ThreadStore.GetStorageEngine().GetThread(board, threadIdStr);

                MemoryStream memIO = new MemoryStream();

                ZipOutputStream zipStream = new ZipOutputStream(memIO);
                zipStream.SetLevel(0); // no compression is needed since most of the files are media files, and they are already compressed anyway

                write_file_to_zip(zipStream, "res/blue.css", Encoding.UTF8.GetBytes(Properties.Resources.css_blue));
                write_file_to_zip(zipStream, "res/sticky.png", Properties.Resources.sticky);
                write_file_to_zip(zipStream, "res/locked.png", Properties.Resources.locked);

                foreach (PostFormatter pf in thread_data)
                {
                    if (pf.MyFile != null)
                    {

                        string full_path;
                        if (FileOperations.ResolveFullFilePath(pf.MyFile.Hash, pf.MyFile.Extension, out full_path))
                        {
                            string ext = Path.GetExtension(full_path);

                            if (!string.IsNullOrEmpty(ext))
                            {
                                ext = ext.Substring(1);
                            }

                            if (ext != pf.MyFile.Extension)
                            {
                                pf.MyFile.ChangeExtension(ext);
                            }

                            string zip_file_name = string.Format("file/{0}.{1}", pf.MyFile.Hash, ext);

                            byte[] data = File.ReadAllBytes(full_path);

                            pf.MyFile.Size = data.Length;

                            write_file_to_zip(zipStream, zip_file_name, data);
                        }

                        string thumb_path;

                        if (FileOperations.CheckThumbFileExist(pf.MyFile.Hash, out thumb_path))
                        {
                            string zip_file_name = string.Format("thumb/{0}.jpg", pf.MyFile.Hash);
                            write_file_to_zip(zipStream, zip_file_name, File.ReadAllBytes(thumb_path));
                        }
                    }
                }

                string notes = ThreadStore.GetStorageEngine().GetThreadNotes(board, threadId);

                string pageHtml = build_page_html(board, threadIdStr, thread_data, notes);

                write_file_to_zip(zipStream, "index.html", Encoding.UTF8.GetBytes(pageHtml));

                zipStream.Close();
                memIO.Close();

                byte[] result = memIO.ToArray();

                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = ServerConstants.ZipContentType;
                response.ContentLength = result.Length;
                response.AddHeader("content-disposition", string.Format("attachment; filename=\"{0}.zip\"", threadId));
                response.SendHeaders();
                response.SendBody(result);

                return true;
            }
            return false;
        }

        private void write_file_to_zip(ZipOutputStream target, string filename, byte[] filedata)
        {
            ZipEntry entry = new ZipEntry(filename);
            entry.DateTime = DateTime.Now;
            entry.Size = filedata.LongLength;
            target.PutNextEntry(entry);
            target.Write(filedata, 0, filedata.Length);
            target.CloseEntry();
        }

        public static string GetLinkToThisPage(string board, int threadid)
        {
            return string.Format("{0}?{1}={2}&{3}={4}", Url, UrlParameters.Board, board, UrlParameters.ThreadId, threadid.ToString());
        }

        private string build_page_html(string board, string threadId, PostFormatter[] thread_data, string notes)
        {
            StringBuilder pageHtml = new StringBuilder(Properties.Resources.zip_page_template);

            StringBuilder body = new StringBuilder();
            {
                body.Append(thread_data[0].ToString(true));

                body.Replace("{post:link}", string.Format("#p{0}", thread_data[0].PostID));

                for (int i = 1; i < thread_data.Length; i++)
                {
                    body.Append(thread_data[i].ToString(true));
                }
            }

            pageHtml.Replace("{title}", string.Format("/{0}/ Thread No. {1}", board, threadId));

            pageHtml.Replace("{board-letter}", board);

            pageHtml.Replace("{notes}", System.Web.HttpUtility.HtmlEncode(notes));

            pageHtml.Replace("{thread-id}", threadId);

            pageHtml.Replace("{thread-posts}", body.ToString());

            return pageHtml.ToString();
        }

    }
}
