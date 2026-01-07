using System;
using Zenject;

namespace Modules.SignalR.Config
{
    public sealed class GameHubConfigProvider : IGameHubConfigProvider
    {
        private readonly GameHubConfigProviderAsset _asset;
        private readonly ITokenProvider _tokenProvider;

        public GameHubConfigProvider(GameHubConfigProviderAsset asset, ITokenProvider tokenProvider)
        {
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _tokenProvider = tokenProvider;
        }

        public Uri GetHubUri()
        {
            return _asset.GetHubUri();
        }

        public string GetAccessToken()
        {
            var token = _tokenProvider?.GetToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            return _asset.GetAccessToken();
        }
    }
}
