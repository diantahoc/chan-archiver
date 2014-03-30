using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class ThreadFiltersPageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command == "/filters")
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < Program.active_dumpers.Count; i++)
                {
                    try
                    {
                        BoardWatcher bw = Program.active_dumpers.ElementAt(i).Value;
                        ChanArchiver.Filters.IFilter[] Filters = bw.Filters;

                        for (int index = 0; index < Filters.Length; index++)
                        {
                            ChanArchiver.Filters.IFilter f = Filters[index];

                            sb.Append("<tr>");

                            sb.AppendFormat("<td>/{0}/</td>", bw.Board);
                            sb.AppendFormat("<td>{0}</td>", f.GetType().FullName.Split('.').Last());
                            sb.AppendFormat("<td><code>{0}</code></td>", f.FilterText);

                            //sb.AppendFormat("<td><a class='label label-warning' href='/filters/?mode=edit&b={0}&i={1}'>Edit</a></td>", bw.Board, index);
                            sb.AppendFormat("<td><a class='label label-danger' href='/filters/?mode=delete&b={0}&i={1}'>Delete</a></td>", bw.Board, index);

                            sb.Append("</tr>");
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

                byte[] data = Encoding.UTF8.GetBytes
                    (Properties.Resources.filters_page
                    .Replace("{blist}", ThreadServerModule.get_board_list("b"))
                    .Replace("{AvFilters}", get_available_filters())
                    .Replace("{Items}", sb.ToString())
                    );

                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;
            }

            if (command.StartsWith("/filters/"))
            {
                string mode = request.QueryString["mode"].Value;
                string board = request.QueryString["b"].Value;

                BoardWatcher bw;

                switch (mode)
                {
                    case "add":
                        if (Program.active_dumpers.ContainsKey(board))
                        {
                            bw = Program.active_dumpers[board];
                        }
                        else
                        {
                            bw = new BoardWatcher(board);
                            Program.active_dumpers.Add(board, bw);
                        }

                        string filter_type = request.QueryString["type"].Value;
                        string filter_exp = request.QueryString["exp"].Value;

                        if (string.IsNullOrEmpty(filter_exp) || string.IsNullOrEmpty(filter_type))
                        {
                            return false;
                        }
                        else
                        {
                            ChanArchiver.Filters.IFilter f = get_filter(filter_type, filter_exp);
                            if (f != null)
                            {
                                bw.AddFilter(f);
                                bw.SaveFilters();
                                response.Redirect("/filters");
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                    case "edit":
                        return false;
                    case "delete":
                        if (Program.active_dumpers.ContainsKey(board))
                        {
                            string index = request.QueryString["i"].Value;

                            bw = Program.active_dumpers[board];

                            int inde = -1;
                            Int32.TryParse(index, out inde);

                            if (inde >= 0 && inde <= bw.Filters.Length - 1)
                            {
                                bw.RemoveFilter(inde);
                                bw.SaveFilters();
                                response.Redirect("/filters");
                                return true;

                            }
                            else { return false; }
                        }
                        else { return false; }

                    default:
                        return false;
                }

            }

            return false;
        }

        private ChanArchiver.Filters.IFilter get_filter(string type, string ext)
        {
            Type t = Type.GetType(type);
            if (t != null)
            {
                System.Reflection.ConstructorInfo ci = t.GetConstructor(new Type[] { typeof(string) });
                ChanArchiver.Filters.IFilter fil = (ChanArchiver.Filters.IFilter)ci.Invoke(new object[] { ext });
                return fil;
            }
            else
            {
                return null;
            }
        }

        private string get_available_filters()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("<option value='{0}'>{1}</option>", "ChanArchiver.Filters.CommentFilter", "Comment Filter (case insensitive)");
            sb.AppendFormat("<option value='{0}'>{1}</option>", "ChanArchiver.Filters.EmailFilter", "Email Filter");
            sb.AppendFormat("<option value='{0}'>{1}</option>", "ChanArchiver.Filters.NameFilter", "Name Filter");
            sb.AppendFormat("<option value='{0}'>{1}</option>", "ChanArchiver.Filters.SubjectFilter", "Subject Filter (case insensitive)");
            sb.AppendFormat("<option value='{0}'>{1}</option>", "ChanArchiver.Filters.TripFilter", "Trip Filter");
            sb.AppendFormat("<option value='{0}'>{1}</option>", "ChanArchiver.Filters.FileHashFilter", "FileHash Filter (not regex)");
            sb.AppendFormat("<option value='{0}'>{1}</option>", "ChanArchiver.Filters.FileNameFilter", "FileName Filter (case insensitive)");

            return sb.ToString();
        }



    }
}
