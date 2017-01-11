using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ChanArchiver.HttpServerHandlers.PageHandlers
{
    public abstract class ThreadFiltersPageHandler
        : PageHandlerBase
    {

        protected abstract Filters.FilterType GetFilterType();

        protected abstract string GetPageUrl();

        public sealed override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            if (request.UriPath == GetPageUrl())
            {
                StringBuilder sb = new StringBuilder(HtmlTemplates.ThreadFiltersPageTemplate);

                IncludeCommonHtml(sb);

                sb.Replace("{{inline-title}}", GetPageTitle());
                sb.Replace("{{boards-list-html}}", ThreadServerModule.get_board_list(UrlParameters.Board));
                sb.Replace("{{available-filters-list}}", get_available_filters());
                sb.Replace("{{filters-list}}", GetThreadFiltersTableHtml());

                WriteFinalHtmlResponse(response, sb.ToString());

                return true;
            }

            if (request.UriPath.StartsWith(GetPageUrl() + "/"))
            {
                string mode = request.QueryString["mode"].Value;
                string board = request.QueryString[UrlParameters.Board].Value;

                BoardWatcher bw;

                switch (mode)
                {
                    case "add":
                        {
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
                                    response.Redirect(GetPageUrl());
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    case "edit":
                        return false;
                    case "delete":
                        {
                            if (Program.active_dumpers.ContainsKey(board))
                            {
                                string index = request.QueryString["i"].Value;

                                bw = Program.active_dumpers[board];

                                int inde = -1;
                                Int32.TryParse(index, out inde);

                                if (inde >= 0 && inde <= bw.WhitelistFilters.Length - 1)
                                {
                                    bw.RemoveFilter(inde);
                                    bw.SaveFilters();
                                    response.Redirect(GetPageUrl());
                                    return true;
                                }
                                else { return false; }
                            }
                            else { return false; }
                        }
                    case "editnotes":
                        {
                            string fID = request.QueryString["filterindex"].Value;
                            string notes_text = request.QueryString["notestext"].Value;

                            if (string.IsNullOrEmpty(fID) || string.IsNullOrEmpty(board))
                            {
                                response.Redirect(GetPageUrl());
                                return true;
                            }
                            else
                            {
                                if (Program.active_dumpers.ContainsKey(board))
                                {
                                    bw = Program.active_dumpers[board];

                                    int index = -1;

                                    Int32.TryParse(fID, out index);

                                    if (index >= 0 && index < bw.WhitelistFilters.Length)
                                    {
                                        Filters.IFilter fil = bw.WhitelistFilters[index];

                                        fil.Notes = notes_text;
                                        bw.SaveFilters();
                                        response.Redirect(GetPageUrl());
                                        return true;
                                    }
                                }
                            }
                        }
                        return false;
                    default:
                        return false;
                }

            }

            return false;
        }

        protected string GetThreadFiltersTableHtml()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Program.active_dumpers.Count; i++)
            {
                try
                {
                    BoardWatcher bw = Program.active_dumpers.ElementAt(i).Value;
                    ChanArchiver.Filters.IFilter[] Filters = GetFilterType() == ChanArchiver.Filters.FilterType.WhiteList
                        ? bw.WhitelistFilters : bw.BlacklistFilters;

                    for (int index = 0; index < Filters.Length; index++)
                    {
                        ChanArchiver.Filters.IFilter f = Filters[index];

                        sb.Append("<tr>");

                        sb.AppendFormat("<td>/{0}/</td>", bw.Board);
                        sb.AppendFormat("<td>{0}</td>", f.GetType().FullName.Split('.').Last());
                        sb.AppendFormat("<td><code>{0}</code></td>", HttpUtility.HtmlEncode(f.FilterText));

                        {
                            StringBuilder notes_form = new StringBuilder();

                            notes_form.Append("<div class=\"input-group\">");

                            notes_form.Append("<form action='/filters/'>");

                            notes_form.AppendFormat("<input type='hidden' name='{0}' value='{1}' />", "mode", "editnotes");

                            notes_form.AppendFormat("<input type='hidden' name='{0}' value='{1}' />", "b", bw.Board);

                            notes_form.AppendFormat("<input type='hidden' name='{0}' value='{1}' />", "filterindex", index);

                            notes_form.Append("<textarea class='form-control' cols='6' rows='3' name='notestext'>");

                            notes_form.Append(HttpUtility.HtmlEncode(f.Notes));

                            notes_form.Append("</textarea>");

                            //notes_form.Append("<br/>");

                            notes_form.Append("<span class=\"input-group-btn\"><button type='submit' class='btn btn-default'>Save</button></span>");

                            notes_form.Append("</form></div>");

                            sb.AppendFormat("<td>{0}</td>", notes_form.ToString());
                        }

                        //sb.AppendFormat("<td><a class='label label-warning' href='/filters/?mode=edit&b={0}&i={1}'>Edit</a></td>", bw.Board, index);
                        sb.AppendFormat("<td><a class=\"btn btn-default\" title='Delete' href='/filters/?mode=delete&b={0}&i={1}'><i class=\"fa fa-trash-o\"></i></a></td>", bw.Board, index);

                        sb.Append("</tr>");
                    }

                }
                catch (Exception)
                {
                    if (i >= Program.active_dumpers.Count) { break; }
                }
            }
            return sb.ToString();
        }

        protected ChanArchiver.Filters.IFilter get_filter(string type, string ext)
        {
            Type t = Type.GetType(type);
            if (t != null)
            {
                System.Reflection.ConstructorInfo ci = t.GetConstructor(new Type[] { typeof(string) });
                ChanArchiver.Filters.IFilter fil = (ChanArchiver.Filters.IFilter)ci.Invoke(new object[] { ext });
                fil.Type = this.GetFilterType();
                return fil;
            }
            else
            {
                return null;
            }
        }

        protected string get_available_filters()
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

    public class WhitelistThreadFiltersPageHandler
        : ThreadFiltersPageHandler
    {
        public const string Url = "/whitefilters";

        protected override Filters.FilterType GetFilterType()
        {
            return Filters.FilterType.WhiteList;
        }

        protected override string GetPageUrl()
        {
            return WhitelistThreadFiltersPageHandler.Url;
        }

        public override string GetPageTitle()
        {
            return "Whitelist Thread Filters";
        }

        public override PageType GetPageType()
        {
            return PageType.WhitelistFilters;
        }
    }

    public class BlacklistThreadFiltersPageHandler
        : ThreadFiltersPageHandler
    {
        public const string Url = "/blackfilters";

        protected override Filters.FilterType GetFilterType()
        {
            return Filters.FilterType.BlackList;
        }

        protected override string GetPageUrl()
        {
            return BlacklistThreadFiltersPageHandler.Url;
        }

        public override string GetPageTitle()
        {
            return "Blacklist Thread Filters";
        }

        public override PageType GetPageType()
        {
            return PageType.BlacklistFilters;
        }
    }
}
