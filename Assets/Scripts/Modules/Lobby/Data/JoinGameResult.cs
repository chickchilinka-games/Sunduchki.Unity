using System;
using System.Collections.Generic;

namespace Modules.Lobby.Data
{
    public struct JoinGameResult
    {
        public string PlayerId { get; }
        public string PlayerName { get; }
        public IReadOnlyList<LobbyPlayerInfo> Players { get; }
        public int DeckCount { get; }
        public int TotalCards { get; }

        public JoinGameResult(
            string playerId,
            string playerName,
            IReadOnlyList<LobbyPlayerInfo> players,
            int deckCount,
            int totalCards)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            Players = players ?? Array.Empty<LobbyPlayerInfo>();
            DeckCount = deckCount;
            TotalCards = totalCards;
        }
    }
}
