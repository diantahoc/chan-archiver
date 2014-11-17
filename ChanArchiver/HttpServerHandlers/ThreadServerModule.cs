using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ChanArchiver
{
    public class ThreadServerModule : HttpServer.HttpModules.HttpModule
    {
        private const int ThreadPerPage = 15;

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
                    string board = parame[2]; if (string.IsNullOrEmpty(board))
                    {
                        response.Redirect("/boards");
                        return true;
                    }

                    int board_thread_count = ThreadStore.CountThreads(board);

                    if (board_thread_count == 0)
                    {
                        response.Redirect("/boards");
                        return true;
                    }

                    int rem = (board_thread_count % ThreadPerPage);

                    int page_count = ((board_thread_count - rem) / ThreadPerPage) + (rem > 0 ? 1 : 0);

                    if (page_count <= 0) { page_count = 1; }

                    int page_offset = 0;

                    Int32.TryParse(request.QueryString["pn"].Value, out page_offset); page_offset = Math.Abs(page_offset);

                    int start = page_offset * (ThreadPerPage);

                    PostFormatter[] board_index = ThreadStore.GetIndex(board, start, ThreadPerPage);

                    StringBuilder s = new StringBuilder();

                    foreach (var pf in board_index)
                    {
                        s.Append("<div class='row'>");
                        s.Append
                            (
                                   pf.ToString()
                                  .Replace("{post:link}", string.Format("/boards/{0}/{1}", board, pf.PostID))
                            );
                        s.Append("</div>");
                    }

                    StringBuilder page_numbers = new StringBuilder();

                    for (int i = 0; i < page_count; i++)
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
                        .Replace("{po}", Convert.ToString(page_offset == 0 ? 0 : page_offset - 1))
                        .Replace("{no}", Convert.ToString(page_offset == page_count - 1 ? page_count : page_offset + 1))
                        .Replace("{pagen}", page_numbers.ToString())
                        .Replace("{Items}", s.ToString()));

                    response.ContentType = "text/html; charset=utf-8";
                    response.Encoding = Encoding.UTF8;
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

                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentLength = respon.Length;
                    response.Encoding = Encoding.UTF8;
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
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength = data.Length;
                response.Encoding = Encoding.UTF8;
                response.SendHeaders();
                response.SendBody(data);
                return true;
            }

            if (command.StartsWith("/deletethread/?"))
            {
                string board = request.QueryString["board"].Value;

                string threadid = request.QueryString["thread"].Value;

                if (string.IsNullOrEmpty(board) || string.IsNullOrEmpty(threadid)) { _404(response); }

                PostFormatter[] thread_data = ThreadStore.GetThread(board, threadid);


                //make sure file index is up-to-date
                Program.update_file_index();

                //delete the files
                foreach (var post in thread_data)
                {
                    if (post.MyFile == null) { continue; }

                    var state = Program.get_file_index_state(post.MyFile.Hash);

                    if (state == null) { continue; }

                    if (state.RepostCount == 1)
                    {
                        string path = Path.Combine(Program.file_save_dir, post.MyFile.Hash + "." + post.MyFile.Extension);

                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        else if (File.Exists(path + ".webm"))
                        {
                            File.Exists(path + ".webm");
                        }

                        path = Path.Combine(Program.thumb_save_dir, post.MyFile.Hash + ".jpg");

                        File.Delete(path);
                    }
                }

                //delete the thread
                ThreadStore.DeleteThread(board, threadid);

                Program.update_file_index();

                response.Redirect("/boards/" + board);
                return true;
            }

            if (command == "/boards" || command == "/boards/")
            {
                if (Directory.Exists(Program.post_files_dir))
                {
                    StringBuilder s = new StringBuilder();

                    foreach (string folder_path in Directory.EnumerateDirectories(Program.post_files_dir))
                    {
                        string folder_name = Path.GetFileName(folder_path);

                        s.Append("<div class=\"col-6 col-sm-6 col-lg-4\">");

                        s.AppendFormat("<h2>/{0}/</h2>", folder_name);
                        s.AppendFormat("<p>Thread Count: {0}</p>", ThreadStore.CountThreads(folder_name));

                        s.AppendFormat("<p><a class=\"btn btn-default\" href=\"/boards/{0}\" role=\"button\">browse »</a></p>", folder_name);

                        s.Append("</div>");
                    }

                    byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.archivedboard_page.Replace("{Items}", s.ToString()));

                    response.Encoding = System.Text.Encoding.UTF8;
                    response.ContentType = "text/html; charset=utf-8";
                    response.Status = System.Net.HttpStatusCode.OK;
                    response.ContentLength = data.Length;
                    response.SendHeaders();
                    response.SendBody(data);
                }
                else
                {
                    response.Redirect("/");
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
                    Program.dump_files(s.Value.PostFile, s.Value.IsThumbOnly);
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
                    if (mon_type == "harvest") { m = BoardWatcher.BoardMode.Harvester; }

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

                    bool thumbOnly = request.QueryString["to"].Value == "1";

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
                        Program.archive_single(board, id, thumbOnly);
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

            if (command.StartsWith("/action/removethreadworker/"))
            {
                string board = request.QueryString["board"].Value;
                string tid = request.QueryString["id"].Value;

                if (Program.active_dumpers.ContainsKey(board))
                {
                    BoardWatcher bw = Program.active_dumpers[board];

                    int id = -1;
                    Int32.TryParse(tid, out id);

                    if (bw.watched_threads.ContainsKey(id))
                    {
                        ThreadWorker tw = bw.watched_threads[id];
                        tw.Stop();
                        bw.watched_threads.Remove(id);
                    }
                }

                response.Redirect("/wjobs");

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
                    Program.dump_files(f.PostFile, f.IsThumbOnly);
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

            if (command.StartsWith("/action/banfile"))
            {
                string hash = request.QueryString["hash"].Value;

                if (!string.IsNullOrEmpty(hash))
                {
                    Program.ban_file(hash);
                }

                string referrer = request.Headers["referer"];

                if (!string.IsNullOrEmpty(referrer)) { response.Redirect(referrer); }

                return true;
            }

            if (command.StartsWith("/action/showfilereposts"))
            {
                string hash = request.QueryString["hash"].Value;

                if (!string.IsNullOrEmpty(hash))
                {

                    Program.update_file_index();

                    Program.FileIndexInfo info = Program.get_file_index_state(hash);

                    if (info != null)
                    {
                        StringBuilder sb = new StringBuilder();

                        var rposts = info.GetRepostsData();

                        for (int i = 0; i < rposts.Length; i++)
                        {
                            sb.Append("<tr>");

                            sb.AppendFormat("<td>{0}</td>", rposts[i].FileName);

                            sb.AppendFormat("<td><a href='/boards/{0}'></a>{0}</td>", rposts[i].Board);

                            sb.AppendFormat("<td><code><a href='/boards/{0}/{1}'>{1}</a></code></td>", rposts[i].Board, rposts[i].ThreadID);

                            sb.AppendFormat("<td><code><a href='/boards/{0}/{1}#p{2}'>{2}</a></code></td>", rposts[i].Board, rposts[i].ThreadID, rposts[i].PostID);

                            sb.Append("</tr>");
                        }

                        write_text(Properties.Resources.file_reposts_page
                            .Replace("{thumbsource}", string.Format("/thumb/{0}.jpg", hash))
                            .Replace("{md5}", hash)
                            .Replace("{rinfo}", sb.ToString()), response);
                        return true;
                    }


                }
                else
                {
                    _404(response);
                    return true;
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

            if (command.StartsWith("/action/forcestopfile_thread/"))
            {
                string workid = command.Split('/').Last();

                FileQueueStateInfo f = Program.get_file_state(workid);

                if (f != null)
                {
                    f.ThreadBG.Cancel(true);
                    response.Redirect("/fileinfo/" + workid);
                }
                else
                {
                    response.Redirect("/fq");
                }

                return true;
            }

            if (command.StartsWith("/action/unbanfile"))
            {
                string hash = request.QueryString["hash"].Value;

                Program.unban_file(hash);

                response.Redirect("/bannedfiles");

                return true;
            }

            #endregion

            if (command.StartsWith("/action/enablefullfile"))
            {
                Settings.ThumbnailOnly = false;
                response.Redirect("/");
                return true;
            }

            if (command == "/ua")
            {
                string ua = request.Headers["User-Agent"].ToLower();
                write_text(string.Format("Your user agent: {0} <br/> Device support webm {1}", ua,
                    ChanArchiver.HttpServerHandlers.FileHandler.device_not_support_webm(ua)), response);
                return true;
            }

            if (command.StartsWith("/stopallmat/"))
            {
                string board = request.QueryString["b"].Value;
                if (!string.IsNullOrWhiteSpace(board))
                {
                    if (Program.active_dumpers.ContainsKey(board))
                    {
                        var bw = Program.active_dumpers[board];

                        for (int i = 0; i < bw.watched_threads.Count; i++)
                        {
                            try
                            {
                                ThreadWorker tw = bw.watched_threads.ElementAt(i).Value;
                                if (!tw.AddedAutomatically)
                                {
                                    tw.Stop();
                                }
                            }
                            catch (System.IndexOutOfRangeException)
                            {
                                break;
                            }
                            catch { }
                        }
                    }
                }
                response.Redirect("/monboards");
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
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength = data.Length;
            response.SendHeaders();
            response.SendBody(data);
        }

        public static string get_board_list(string name)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("<select name=\"{0}\" class=\"form-control\">", name);

            foreach (var bb in Program.ValidBoards)
            {
                sb.AppendFormat("<option value='{0}'>{0} - {1}</option>", bb.Key, bb.Value.Title);
            }

            sb.Append("</select>");
            return sb.ToString();
        }
    }
}
