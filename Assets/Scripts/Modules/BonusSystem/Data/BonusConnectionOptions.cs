using System;

namespace Modules.BonusSystem.Data
{
    public readonly struct BonusConnectionOptions
    {
        public Uri HubUri { get; }
        public string AccessToken { get; }
        public string GameId { get; }
        public string PlayerId { get; }

        public BonusConnectionOptions(Uri hubUri, string accessToken, string gameId, string playerId)
        {
            HubUri = hubUri ?? throw new ArgumentNullException(nameof(hubUri));
            AccessToken = accessToken ?? string.Empty;
            GameId = gameId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
        }
    }
}
