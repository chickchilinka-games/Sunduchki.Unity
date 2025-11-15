using System;

namespace Modules.Lobby.Data
{
    public struct LobbyApiConfig
    {
        public Uri BaseAddress { get; }

        public LobbyApiConfig(Uri baseAddress)
        {
            BaseAddress = baseAddress;
        }
    }
}
