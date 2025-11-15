using System;

namespace Modules.TurnSystem.Data
{
    public readonly struct TurnTrackingOptions
    {
        public Uri HubUri { get; }
        public string AccessToken { get; }
        public string GameId { get; }
        public string PlayerId { get; }

        public TurnTrackingOptions(Uri hubUri, string accessToken, string gameId, string playerId)
        {
            HubUri = hubUri ?? throw new ArgumentNullException(nameof(hubUri));
            AccessToken = accessToken ?? string.Empty;
            GameId = gameId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
        }
    }
}
