using System;
using UnityEngine;

namespace Modules.SignalR.Config
{
    [CreateAssetMenu(menuName = "Sunduchki/SignalR/Game Hub Config", fileName = "GameHubConfig")]
    public class GameHubConfigProviderAsset : ScriptableObject, IGameHubConfigProvider
    {
        [SerializeField] private string _hubUrl = "http://localhost:5000/hub/game";
        [SerializeField] private string _accessToken = string.Empty;

        private Uri _cachedUri;

        public Uri GetHubUri()
        {
            if (_cachedUri != null)
            {
                return _cachedUri;
            }

            if (string.IsNullOrWhiteSpace(_hubUrl) || !Uri.TryCreate(_hubUrl, UriKind.Absolute, out _cachedUri))
            {
                throw new InvalidOperationException("Game hub url is not configured.");
            }

            return _cachedUri;
        }

        public string GetAccessToken()
        {
            return _accessToken;
        }
    }
}
