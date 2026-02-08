using System;
using Modules.AuthenticationSystem.Config;
using UnityEngine;

namespace Modules.SignalR.Config
{
    [CreateAssetMenu(menuName = "Sunduchki/SignalR/Game Hub Config", fileName = "GameHubConfig")]
    public class GameHubConfigProviderAsset : ScriptableObject, IGameHubConfigProvider
    {
        [SerializeField] private BaseUrlConfigAsset _baseConfig;
        [SerializeField] private string _relativePath = "hub/game";
        [SerializeField, HideInInspector] private string _hubUrl = "http://localhost:5000/game/hub/game";
        [SerializeField] private string _accessToken = string.Empty;

        private Uri _cachedUri;

        public Uri GetHubUri()
        {
            if (_cachedUri != null)
            {
                return _cachedUri;
            }

            if (_baseConfig != null)
            {
                _cachedUri = CombinePath(_baseConfig.GetGameBaseUri(), _relativePath);
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

        private static Uri CombinePath(Uri baseUri, string relativePath)
        {
            if (baseUri == null)
            {
                throw new InvalidOperationException("Game hub base url is not configured.");
            }

            var trimmedBase = baseUri.AbsolutePath.TrimEnd('/');
            var trimmedRelative = (relativePath ?? string.Empty).TrimStart('/');
            var builder = new UriBuilder(baseUri)
            {
                Path = $"{trimmedBase}/{trimmedRelative}"
            };
            builder.Query = string.Empty;
            return builder.Uri;
        }
    }
}
