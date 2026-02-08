using System;
using Modules.AuthenticationSystem.Config;
using Modules.Lobby.Data;

namespace Modules.Lobby.Config
{
    public sealed class LobbyApiConfigProvider : ILobbyApiConfigProvider
    {
        private readonly BaseUrlConfigAsset _baseConfig;

        public LobbyApiConfigProvider(BaseUrlConfigAsset baseConfig)
        {
            _baseConfig = baseConfig ?? throw new ArgumentNullException(nameof(baseConfig));
        }

        public LobbyApiConfig GetConfig()
        {
            var baseUri = _baseConfig.GetGameBaseUri();
            return new LobbyApiConfig(baseUri);
        }
    }
}
