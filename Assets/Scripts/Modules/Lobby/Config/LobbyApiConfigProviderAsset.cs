using System;
using Modules.AuthenticationSystem.Config;
using Modules.Lobby.Data;
using UnityEngine;

namespace Modules.Lobby.Config
{
    [CreateAssetMenu(fileName = "LobbyApiConfigProvider", menuName = "Sunduchki/Lobby/ApiConfigProvider")]
    public class LobbyApiConfigProviderAsset : ScriptableObject, ILobbyApiConfigProvider
    {
        [SerializeField]
        private BaseUrlConfigAsset _baseConfig;

        [SerializeField, HideInInspector]
        private string _baseAddress = "http://localhost:5000/game";

        public LobbyApiConfig GetConfig()
        {
            var baseUri = ResolveBaseUri();
            return new LobbyApiConfig(baseUri);
        }

        private Uri ResolveBaseUri()
        {
            if (_baseConfig != null)
            {
                return _baseConfig.GetGameBaseUri();
            }

            if (Uri.TryCreate(_baseAddress, UriKind.Absolute, out var parsed))
            {
                return parsed;
            }

            return new Uri("http://localhost:5000/game");
        }
    }
}
