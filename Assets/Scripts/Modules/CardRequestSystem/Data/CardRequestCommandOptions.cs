using System;

namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardRequestCommandOptions
    {
        public Uri HubUri { get; }
        public string AccessToken { get; }
        public string GameId { get; }
        public string PlayerId { get; }

        public CardRequestCommandOptions(Uri hubUri, string accessToken, string gameId, string playerId)
        {
            HubUri = hubUri;
            AccessToken = accessToken ?? string.Empty;
            GameId = gameId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
        }
    }
}
