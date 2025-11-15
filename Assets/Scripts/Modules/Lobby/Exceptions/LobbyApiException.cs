using System;
using System.Net;

namespace Modules.Lobby.Exceptions
{
    public class LobbyApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public LobbyApiException(HttpStatusCode statusCode, string responseBody, string message)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
