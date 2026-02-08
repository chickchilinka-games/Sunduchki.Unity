using System;
using Modules.AuthenticationSystem.Config;
using Zenject;

namespace Modules.SignalR.Config
{
    public sealed class GameHubConfigProvider : IGameHubConfigProvider
    {
        private readonly BaseUrlConfigAsset _baseConfig;
        private readonly ITokenProvider _tokenProvider;
        private const string HubPath = "hub/game";

        public GameHubConfigProvider(BaseUrlConfigAsset baseConfig, ITokenProvider tokenProvider)
        {
            _baseConfig = baseConfig ?? throw new ArgumentNullException(nameof(baseConfig));
            _tokenProvider = tokenProvider;
        }

        public Uri GetHubUri()
        {
            var baseUri = _baseConfig.GetGameBaseUri();
            return CombinePath(baseUri, HubPath);
        }

        public string GetAccessToken()
        {
            var token = _tokenProvider?.GetToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            return string.Empty;
        }

        private static Uri CombinePath(Uri baseUri, string relativePath)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
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
