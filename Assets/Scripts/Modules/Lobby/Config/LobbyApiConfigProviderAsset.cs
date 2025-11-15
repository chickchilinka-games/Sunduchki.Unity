using System;
using Modules.Lobby.Data;
using UnityEngine;

namespace Modules.Lobby.Config
{
    [CreateAssetMenu(fileName = "LobbyApiConfigProvider", menuName = "Sunduchki/Lobby/ApiConfigProvider")]
    public class LobbyApiConfigProviderAsset : ScriptableObject, ILobbyApiConfigProvider
    {
        [SerializeField]
        private string _baseAddress = "http://localhost:5000";

        public LobbyApiConfig GetConfig()
        {
            var uri = Uri.TryCreate(_baseAddress, UriKind.Absolute, out var parsed)
                ? parsed
                : new Uri("http://localhost:5000");

            return new LobbyApiConfig(uri);
        }
    }
}
