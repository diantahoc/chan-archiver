using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChanArchiver.HttpServerHandlers
{
    public class BannedFilesPageHandler : HttpServer.HttpModules.HttpModule
    {
        public override bool Process(HttpServer.IHttpRequest request, HttpServer.IHttpResponse response, HttpServer.Sessions.IHttpSession session)
        {
            string command = request.UriPath;

            if (command == "/bannedfiles")
            {
                StringBuilder sb = new StringBuilder();

                string[] hashes = Program.get_banned_file_list();

                for (int i = 0; i < hashes.Length; i++)
                {
                    sb.AppendFormat("<tr><td><img src='/thumb/{0}.jpg' style='max-width:75px; max-height:75px' /></td><td>{0}</td><td><a class=\"btn btn-default\" href='/action/unbanfile?hash={0}' title='Remove'><i class=\"fa fa-trash-o\"></i></a></td></tr>", hashes[i]);
                }

                //write everything
                response.Status = System.Net.HttpStatusCode.OK;
                response.ContentType = "text/html";

                byte[] data = Encoding.UTF8.GetBytes(Properties.Resources.banned_files_page.Replace("{Items}", sb.ToString()));
                response.ContentLength = data.Length;
                response.SendHeaders();
                response.SendBody(data);

                return true;

            }

            return false;
        }
    }
}
