using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AniWrap.DataTypes
{
    public class APIResponse
    {

        public string Data { get; private set; }

        public ErrorType Error { get; private set; }

        public APIResponse(string data, ErrorType type)
        {
            this.Data = data;
            this.Error = type;
        }

        public enum ErrorType
        {
            NoError,
            NotFound,
            Other
        }

    }
}
