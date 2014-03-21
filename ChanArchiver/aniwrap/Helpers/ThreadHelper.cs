using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Web;
using AniWrap.DataTypes;

namespace AniWrap.Helpers
{
    public static class ThreadHelper
    {
        /*public static CommentToken[] TokenizeComment(string comment)
        {
            List<CommentToken> tokens = new List<CommentToken>();

            HtmlDocument d = new HtmlDocument();

            d.LoadHtml(comment);

            foreach (HtmlNode node in d.DocumentNode.ChildNodes)
            {
                switch (node.Name)
                {
                    case "#text":
                        tokens.Add(new CommentToken(CommentToken.TokenType.Text, HttpUtility.HtmlDecode(node.InnerText)));
                        break;
                    case "a":
                        if (node.GetAttributeValue("class", "") == "quotelink")
                        {
                            string inner_text = HttpUtility.HtmlDecode(node.InnerText);
                            if (inner_text.StartsWith(">>>"))
                            {
                                //board redirect (sometimes with a post number)
                                int test_i = -1;
                                try
                                {
                                    test_i = Convert.ToInt32(inner_text.Split('/').Last()); // The last should be a number or an empty string. I guess

                                    //if success, it's a board_thread_redirect OR it's a cross-thread link ( I don't know if 4chan handle both the same way )

                                    string board_letter = inner_text.Replace(">", "").Replace("/", "").Replace(test_i.ToString(), "");

                                    tokens.Add(new CommentToken(CommentToken.TokenType.BoardThreadRedirect, board_letter + "-" + test_i.ToString()));

                                }
                                catch (Exception)
                                {
                                    // it is a plain board redirect such as >>>/g/
                                    tokens.Add(new CommentToken(CommentToken.TokenType.BoardRedirect, inner_text.Replace(">", "").Replace("/", ""))); // ex: >>>/g/ -> g
                                }
                            }
                            else if (inner_text.StartsWith(">>"))
                            {
                                int test_i = -1;
                                try
                                {
                                    test_i = Convert.ToInt32(inner_text.Remove(0, 2));
                                    //it's a post quote link
                                    tokens.Add(new CommentToken(CommentToken.TokenType.Quote, inner_text.Remove(0, 2)));
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            //throw new Exception("Unsupported data type");
                        }
                        break;
                    case "br":
                        tokens.Add(new CommentToken(CommentToken.TokenType.Newline, ""));
                        break;
                    case "wbr":
                        //no action
                        break;
                    case "span":
                        if (node.GetAttributeValue("class", "") == "quote")
                        {
                            tokens.Add(new CommentToken(CommentToken.TokenType.GreenText, HttpUtility.HtmlDecode(node.InnerText)));
                        }
                        else if (node.GetAttributeValue("class", "") == "deadlink")
                        {
                            //dead link
                            string inner_text = HttpUtility.HtmlDecode(node.InnerText);
                            int test_i = -1;
                            try
                            {
                                test_i = Convert.ToInt32(inner_text.Remove(0, 2));
                                //it's a post quote link
                                tokens.Add(new CommentToken(CommentToken.TokenType.DeadLink, inner_text.Remove(0, 2)));
                            }
                            catch (Exception)
                            {
                                throw;
                            }

                        }
                        else if (node.GetAttributeValue("class", "") == "fortune")
                        {
                            string data = HttpUtility.HtmlDecode(node.InnerText);
                            string color = node.GetAttributeValue("style", "");
                            tokens.Add(new CommentToken(CommentToken.TokenType.ColoredFText, "#" + color.Split('#')[1] + "$" + data));
                        }
                        else
                        {
                            //throw new Exception("Unsupported data type");
                        }
                        break;
                    case "pre":
                        if (node.GetAttributeValue("class", "") == "prettyprint")
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (HtmlNode prenode in node.ChildNodes)
                            {
                                if (prenode.Name == "br")
                                {
                                    sb.AppendLine();
                                }
                                else
                                {
                                    sb.Append(prenode.InnerText);
                                }
                            }
                            tokens.Add(new CommentToken(CommentToken.TokenType.CodeBlock, sb.ToString()));
                        }
                        break;
                    case "s":
                        tokens.Add(new CommentToken(CommentToken.TokenType.SpoilerText, HttpUtility.HtmlDecode(node.InnerText)));
                        break;
                    case "small":
                        //Oekaki Post 
                        break;
                    default:
                        //throw new Exception("Unsupported data type");
                        break;
                }
            }
            return tokens.ToArray();
        }
        */
     
        public static string Guess_Post_Title(GenericPost t)
        {
            if (String.IsNullOrEmpty(t.Subject))
            {
                if (String.IsNullOrEmpty(t.Comment))
                {
                    return t.ID.ToString();
                }
                else
                {
                    string comment = "";

                    HtmlAgilityPack.HtmlDocument d = new HtmlAgilityPack.HtmlDocument();
                    d.LoadHtml(t.Comment);

                    comment = HttpUtility.HtmlDecode(d.DocumentNode.InnerText);
                    if (comment.Length > 25)
                    {
                        return comment.Remove(24) + "...";
                    }
                    else
                    {
                        return comment;
                    }
                }
            }
            else
            {
                return HttpUtility.HtmlDecode(t.Subject);
            }
        }
    }
}
