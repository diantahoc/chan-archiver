using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AniWrap.DataTypes
{
    public class CommentToken
    {
        public enum TokenType
        {
            Quote, // plain number 123456
            Text, // text
            SpoilerText, //text
            CodeBlock, //text
            GreenText, // >text
            Newline, // ''
            DeadLink, // plain number
            ColoredFText, //f text - #FFFFF$text
            BoardRedirect, // board_letter (g, b, etc)
            BoardThreadRedirect, //board_letter-tid : g-123
            CatalogRedirect // ?unkown 
        };

        public string TokenData { get; private set; }

        public TokenType Type { get; private set; }

        public CommentToken(TokenType type, string data)
        {
            this.TokenData = data;
            this.Type = type;
        }

    }
}
