using System;
using System.Collections.Generic;

namespace Modules.Lobby.Data
{
    public readonly struct MatchmakingResult
    {
        public string GameId { get; }
        public string PlayerId { get; }
        public IReadOnlyList<LobbyPlayerInfo> Players { get; }
        public int DeckCount { get; }
        public int TotalCards { get; }

        public MatchmakingResult(
            string gameId,
            string playerId,
            IReadOnlyList<LobbyPlayerInfo> players,
            int deckCount,
            int totalCards)
        {
            GameId = gameId;
            PlayerId = playerId;
            Players = players ?? Array.Empty<LobbyPlayerInfo>();
            DeckCount = deckCount;
            TotalCards = totalCards;
        }
    }
}
