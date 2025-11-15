using System;

namespace Modules.Lobby.Data
{
    public struct LobbySignalRConnectionOptions
    {
        public Uri HubUri { get; }
        public string AccessToken { get; }

        public LobbySignalRConnectionOptions(Uri hubUri, string accessToken)
        {
            HubUri = hubUri;
            AccessToken = accessToken;
        }
    }
}
