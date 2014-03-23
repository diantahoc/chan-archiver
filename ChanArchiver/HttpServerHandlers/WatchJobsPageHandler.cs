﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class WatchJobsPageHandler : HttpServer.HttpModules.HttpModule
    {

        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command == "/wjobs")
            {

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < Program.active_dumpers.Count; i++)
                {
                    try
                    {
                        BoardWatcher bw = Program.active_dumpers.ElementAt(i).Value;

                        for (int index = 0; index < bw.watched_threads.Count; index++)
                        {

                            try
                            {
                                ThreadWorker tw = bw.watched_threads.ElementAt(index).Value;

                                //don't pollute the list with unactive thread workers (because no filter match) in monitor only mode
                                if (tw.AddedAutomatically && (!tw.IsActive) && bw.Mode == BoardWatcher.BoardMode.Monitor) { continue; }

                                sb.Append("<tr>");
                                if (tw.IsActive)
                                {
                                    sb.AppendFormat("<td><a class=\"btn btn-warning\" href=\"/cancel/tw/{0}/{1}\">Stop</a></td>", bw.Board, tw.ID);
                                }
                                else
                                {
                                    sb.AppendFormat("<td><a class=\"btn btn-info\" href=\"/cancel/twr/{0}/{1}\">Start</a></td>", bw.Board, tw.ID);
                                }

                                sb.AppendFormat("<td>{0}</td>", string.Format("/{0}/", bw.Board));
                                sb.AppendFormat("<td>{0}</td>", tw.ID); 
                                
                                sb.AppendFormat("<td><code>{0}</code></td>", tw.AddedAutomatically);

                                sb.AppendFormat("<td><a href='/boards/{0}/{1}' class='label label-danger'>*click*</a></td>", bw.Board, tw.ID);


                                sb.AppendFormat("<td>{0}</td>", tw.LastUpdated);

                                sb.AppendFormat("<td> <a href='/logs/{0}/{1}/{2}' class='label label-primary'>Logs</a> </td>", "threadworker", bw.Board, tw.ID);

                                sb.Append("</tr>");
                            }
                            catch (Exception)
                            {
                                if (index >= bw.watched_threads.Count) { break; }
                            }
                        }

                    }
                    catch (Exception)
                    {
                        if (i >= Program.active_dumpers.Count) { break; }
                    }
                }

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.wjobs_page.Replace("{Items}", sb.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }
            return false;
        }

    }
}
