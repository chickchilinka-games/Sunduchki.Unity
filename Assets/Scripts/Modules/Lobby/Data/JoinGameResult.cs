using System;
using System.Collections.Generic;

namespace Modules.Lobby.Data
{
    public struct JoinGameResult
    {
        public string PlayerId { get; }
        public IReadOnlyList<LobbyPlayerInfo> Players { get; }
        public int DeckCount { get; }
        public int TotalCards { get; }
        public bool Started { get; }

        public JoinGameResult(
            string playerId,
            IReadOnlyList<LobbyPlayerInfo> players,
            int deckCount,
            int totalCards,
            bool started)
        {
            PlayerId = playerId;
            Players = players ?? Array.Empty<LobbyPlayerInfo>();
            DeckCount = deckCount;
            TotalCards = totalCards;
            Started = started;
        }
    }
}
