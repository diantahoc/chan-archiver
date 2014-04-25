﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver
{
    public class ThreadServerModule : HttpServer.HttpModules.HttpModule
    {
        private const double ThreadPerPage = 15.0;

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath.ToString();

            #region Thread & Index View

            if (command.StartsWith("/boards/"))
            {
                response.Encoding = System.Text.Encoding.UTF8;

                string[] parame = command.Split('?')[0].Split('/');

                if (parame.Length == 3)
                {
                    //board index view mode
                    string board = parame[2]; if (string.IsNullOrEmpty(board)) { _404(response); return true; }

                    PostFormatter[] board_index = ThreadStore.GetIndex(board); if (board_index.Length == 0) { _404(response); return true; }

                    int page_count = Convert.ToInt32(Math.Round(Convert.ToDouble(board_index.Length) / ThreadPerPage, MidpointRounding.AwayFromZero));

                    if (page_count <= 0) { page_count = 1; }

                    int page_offset = 0;

                    Int32.TryParse(request.QueryString["pn"].Value, out page_offset);

                    page_offset = Math.Abs(page_offset);

                    StringBuilder s = new StringBuilder();

                    int start = Convert.ToInt32(page_offset * (ThreadPerPage - 1));
                    int end = Convert.ToInt32(start + ThreadPerPage);

                    for (int i = start; i < end && i < board_index.Length; i++)
                    {
                        PostFormatter pf = board_index[i];
                        s.Append("<div class='row'>");
                        s.Append
                            (
                                   pf.ToString()
                                  .Replace("{post:link}", string.Format("/boards/{0}/{1}", board, pf.PostID))
                            );
                        s.Append("</div>");
                    }

                    StringBuilder page_numbers = new StringBuilder();

                    for (int i = 0; i < page_count + 3; i++)
                    {
                        if (i == page_offset)
                        {
                            page_numbers.AppendFormat("<li class=\"active\"><a href=\"?pn={0}\">{1}</a></li>", i, i + 1);
                        }
                        else
                        {
                            page_numbers.AppendFormat("<li><a href=\"?pn={0}\">{1}</a></li>", i, i + 1);
                        }
                    }

                    byte[] data = Encoding.UTF8.GetBytes(
                        Properties.Resources.board_index_page
                        .Replace("{po}", Convert.ToString(page_offset - 1))
                        .Replace("{no}", Convert.ToString(page_offset + 1))
                        .Replace("{pagen}", page_numbers.ToString())
                        .Replace("{Items}", s.ToString()));

                    response.ContentType = "text/html";
                    response.Status = System.Net.HttpStatusCode.OK;
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);

                    return true;
                }
                else if (parame.Length >= 4)
                {
                    //thread view mode
                    string board = parame[2];
                    string threadid = parame[3];

                    if (string.IsNullOrEmpty(board) || string.IsNullOrEmpty(threadid)) { _404(response); }

                    PostFormatter[] thread_data = ThreadStore.GetThread(board, threadid); if (thread_data.Length == 0) { _404(response); return true; }

                    StringBuilder body = new StringBuilder();

                    body.Append(thread_data[0]);

                    body.Replace("{post:link}", string.Format("#p{0}", thread_data[0].PostID));

                    for (int i = 1; i < thread_data.Length; i++)
                    {
                        body.Append(thread_data[i]);
                    }

                    //body.Append("</div>");

                    byte[] respon = Encoding.UTF8.GetBytes
                        (Properties.Resources.page_template
                        .Replace("{board-title}", string.Format("/{0}/ - ChanArchiver", board))
                        .Replace("{board-letter}", board)
                        .Replace("{thread-id}", threadid)
                        .Replace("{thread-posts}", body.ToString()));

                    response.ContentLength = respon.Length;

                    response.SendHeaders();
                    response.SendBody(respon);

                    return true;
                }
            }

            if (command.StartsWith("/getfilelist?"))
            {
                string board = request.QueryString["board"].Value;

                string threadid = request.QueryString["thread"].Value;

                if (string.IsNullOrEmpty(board) || string.IsNullOrEmpty(threadid)) { _404(response); }

                PostFormatter[] thread_data = ThreadStore.GetThread(board, threadid); if (thread_data.Length == 0) { _404(response); return true; }

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < thread_data.Length; i++)
                {
                    PostFormatter pf = thread_data[i];
                    if (pf.MyFile != null)
                    {
                        string url_name = System.Web.HttpUtility.UrlEncodeUnicode(pf.MyFile.FileName);
                        string url = string.Format("/filecn/{0}.{1}?cn={2}", pf.MyFile.Hash, pf.MyFile.Extension, url_name);
                        sb.AppendFormat("<a href='{0}'>{1}</a><br/>", url, pf.MyFile.FileName);
                    }
                }

                byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
                response.ContentType = "text/html";
                response.ContentLength = data.Length;
                response.Encoding = Encoding.UTF8;
                response.SendHeaders();
                response.SendBody(data);
                return true;
            }

            if (command == "/boards" || command == "/boards/")
            {
                response.Encoding = System.Text.Encoding.UTF8;

                if (Directory.Exists(Program.post_files_dir))
                {
                    response.ContentType = "text/html";
                    response.Status = System.Net.HttpStatusCode.OK;

                    DirectoryInfo info = new DirectoryInfo(Program.post_files_dir);

                    DirectoryInfo[] folders = info.GetDirectories();

                    StringBuilder s = new StringBuilder();

                    for (int i = 0; i < folders.Length; i++)
                    {
                        s.Append("<div class=\"col-6 col-sm-6 col-lg-4\">");

                        s.AppendFormat("<h2>/{0}/</h2>", folders[i].Name);
                        s.AppendFormat("<p>Thread Count: {0}</p>", folders[i].GetDirectories().Count());

                        s.AppendFormat("<p><a class=\"btn btn-default\" href=\"/boards/{0}\" role=\"button\">browse »</a></p>", folders[i].Name);

                        s.Append("</div>");

                        // s.AppendFormat("<a href='/boards/{0}'>/{0}/</a><br/>", folders[i].Name);
                    }

                    byte[] data = System.Text.Encoding.UTF8.GetBytes(Properties.Resources.archivedboard_page.Replace("{Items}", s.ToString()));

                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);

                }
                else
                {
                    _404(response);
                }

                return true;
            }

            #endregion

            #region File Queue Actions

            if (command.StartsWith("/set/maxfilequeue/"))
            {
                if (string.IsNullOrEmpty(request.QueryString["count"].Value))
                {
                    response.Redirect("/fq");
                }
                else
                {
                    int t = Program.file_stp.MaxThreads;

                    Int32.TryParse(request.QueryString["count"].Value, out t);

                    if (t != Program.file_stp.MaxThreads)
                    {
                        if (t > Program.file_stp.MinThreads)
                        {
                            Program.file_stp.MaxThreads = t;
                        }
                    }
                    response.Redirect("/fq");
                }
                return true;
            }


            if (command == "/action/removecompletefiles")
            {
                List<string> hashes_to_remove = new List<string>();
                for (int index = 0; index < Program.queued_files.Count(); index++)
                {
                    try
                    {
                        FileQueueStateInfo f = Program.queued_files.ElementAt(index).Value;
                        if (f.Status == FileQueueStateInfo.DownloadStatus.Complete)
                        {
                            hashes_to_remove.Add(Program.queued_files.ElementAt(index).Key);
                        }
                    }
                    catch (Exception)
                    {
                        if (index > Program.queued_files.Count()) { break; }
                    }
                }

                foreach (string s in hashes_to_remove)
                {
                    Program.queued_files.Remove(s);
                }

                response.Redirect("/fq");

                return true;
            }

            if (command == "/action/removefailedfiles")
            {
                List<string> hashes_to_remove = new List<string>();
                for (int index = 0; index < Program.queued_files.Count(); index++)
                {
                    try
                    {
                        FileQueueStateInfo f = Program.queued_files.ElementAt(index).Value;
                        if (f.Status == FileQueueStateInfo.DownloadStatus.Error || f.Status == FileQueueStateInfo.DownloadStatus.NotFound)
                        {
                            hashes_to_remove.Add(Program.queued_files.ElementAt(index).Key);
                        }
                    }
                    catch (Exception)
                    {
                        if (index > Program.queued_files.Count()) { break; }
                    }
                }

                foreach (string s in hashes_to_remove)
                {
                    Program.queued_files.Remove(s);
                }

                response.Redirect("/fq");

                return true;
            }

            if (command == "/action/restartfailedfiles")
            {
                List<KeyValuePair<string, FileQueueStateInfo>> files_to_restart = new List<KeyValuePair<string, FileQueueStateInfo>>();

                for (int index = 0; index < Program.queued_files.Count(); index++)
                {
                    try
                    {
                        FileQueueStateInfo f = Program.queued_files.ElementAt(index).Value;
                        if (f.Status == FileQueueStateInfo.DownloadStatus.Error)
                        {
                            files_to_restart.Add(Program.queued_files.ElementAt(index));
                        }
                    }
                    catch (Exception)
                    {
                        if (index > Program.queued_files.Count()) { break; }
                    }
                }

                foreach (KeyValuePair<string, FileQueueStateInfo> s in files_to_restart)
                {
                    Program.queued_files.Remove(s.Key);
                    Program.dump_files(s.Value.PostFile);
                }
                response.Redirect("/fq");

                return true;
            }

            #endregion

            #region Watch Jobs Actions

            if (command.StartsWith("/add/"))
            {
                string[] rdata = command.Split('/');
                string mode = rdata[2].ToLower();

                if (mode == "board")
                {
                    if (string.IsNullOrEmpty(request.QueryString["boardletter"].Value))
                    {
                        _404(response);
                    }

                    string board = request.QueryString["boardletter"].Value;
                    string mon_type = request.QueryString["montype"].Value;

                    BoardWatcher.BoardMode m = BoardWatcher.BoardMode.None;

                    if (mon_type == "part") { m = BoardWatcher.BoardMode.Monitor; }
                    if (mon_type == "full") { m = BoardWatcher.BoardMode.FullBoard; }

                    Program.archive_board(board, m);

                    response.Status = System.Net.HttpStatusCode.OK;

                    response.Redirect("/monboards");

                }
                else if (mode == "thread")
                {
                    if (string.IsNullOrEmpty(request.QueryString["urlorformat"].Value))
                    {
                        _404(response);
                    }

                    string input = request.QueryString["urlorformat"].Value;

                    string board = "";
                    int id = -1;


                    if (input.ToLower().StartsWith("http"))
                    {
                        //http://boards.4chan.org/g/res/39075359
                        string temp = input.ToLower().Replace("https://", "").Replace("http://", "");

                        //boards.4chan.org/g/res/int
                        // 0               1  2  3 
                        string[] data = temp.Split('/');

                        if (data.Length >= 4)
                        {
                            board = data[1];

                            Int32.TryParse(data[3].Split('#')[0], out id);
                        }
                    }
                    else
                    {
                        string[] data = input.Split(':');
                        if (data.Length >= 2)
                        {
                            board = data[0];
                            Int32.TryParse(data[1], out id);
                        }
                    }


                    if (id > 0 & !string.IsNullOrEmpty(board))
                    {
                        Program.archive_single(board, id);
                        response.Status = System.Net.HttpStatusCode.OK;

                        response.Redirect("/wjobs");
                    }
                    else
                    {
                        _404(response);
                    }
                }
                else
                {
                    _404(response);
                }

                return true;
            }

            if (command.StartsWith("/cancel/"))
            {
                string[] data = command.Split('/');
                string mode = data[2];

                if (mode == "bw")
                {
                    string board = data[3];
                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        bw.StopMonitoring();
                        response.Redirect("/monboards");
                    }

                }

                if (mode == "bwr")
                {
                    string board = data[3];
                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        bw.StartMonitoring(BoardWatcher.BoardMode.FullBoard);
                        response.Redirect("/monboards");
                    }
                }

                if (mode == "tw")
                {
                    string board = data[3];
                    string tid = data[4];

                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        int id = Convert.ToInt32(tid);
                        if (bw.watched_threads.ContainsKey(id))
                        {
                            ThreadWorker tw = bw.watched_threads[id];
                            tw.Stop();
                            response.Redirect("/wjobs");
                        }
                    }
                }

                if (mode == "twr")
                {
                    string board = data[3];
                    string tid = data[4];

                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        BoardWatcher bw = Program.active_dumpers[board];
                        int id = Convert.ToInt32(tid);
                        if (bw.watched_threads.ContainsKey(id))
                        {
                            ThreadWorker tw = bw.watched_threads[id];
                            tw.Start();
                            response.Redirect("/wjobs");
                        }
                    }
                }

                return true;
            }

            #endregion

            #region File Actions

            if (command.StartsWith("/action/restartfile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.ForceStop = true;
                    Program.queued_files.Remove(workid);
                    Program.dump_files(f.PostFile);
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/stopandbanfile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.ForceStop = true;
                    Program.ban_file(f.Hash);
                    f.Log(new LogEntry() { Level = LogEntry.LogLevel.Success, Message = "File was banned", Sender = "-", Title = "" });
                    // Program.queued_files.Remove(workid);
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/removefile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    Program.queued_files.Remove(workid);
                    response.Redirect("/fq");
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

           

            if (command.StartsWith("/action/resetfileretrycount/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.RetryCount = 0;
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/forcestopfile/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.ForceStop = true;
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            #endregion

            if (command.StartsWith("/action/enablefullfile"))
            {
                Program.thumb_only = false;
                response.Redirect("/");
                return true;
            }

            return false;
        }

        public static void _404(HttpServer.IHttpResponse response)
        {
            response.Status = System.Net.HttpStatusCode.NotFound;
            byte[] d = System.Text.Encoding.UTF8.GetBytes("404");
            response.ContentLength = d.Length;
            response.SendHeaders();
            response.SendBody(d);
        }

        public static void write_text(string text, HttpServer.IHttpResponse response)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            response.ContentLength = data.Length;
            response.SendHeaders();
            response.SendBody(data);
        }

        public static string get_board_list(string name)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("<select name=\"{0}\" class=\"form-control\">", name);

            foreach (KeyValuePair<string, string> bb in Program.ValidBoards)
            {
                sb.AppendFormat("<option value='{0}'>{0} - {1}</option>", bb.Key, bb.Value);
            }

            sb.Append("</select>");
            return sb.ToString();
        }
    }
}
