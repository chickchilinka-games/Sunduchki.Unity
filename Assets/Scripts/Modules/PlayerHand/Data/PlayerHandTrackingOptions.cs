using System;

namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandTrackingOptions
    {
        public Uri HubUri { get; }
        public string AccessToken { get; }
        public string GameId { get; }
        public string PlayerId { get; }

        public PlayerHandTrackingOptions(Uri hubUri, string accessToken, string gameId, string playerId)
        {
            HubUri = hubUri ?? throw new ArgumentNullException(nameof(hubUri));
            AccessToken = accessToken ?? string.Empty;
            GameId = gameId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
        }
    }
}
