namespace Modules.Lobby.Data
{
    public struct LobbyGameStartedPayload
    {
        public string GameId { get; }
        public int? DeckCount { get; }
        public int? TotalCards { get; }

        public LobbyGameStartedPayload(string gameId, int? deckCount, int? totalCards)
        {
            GameId = gameId;
            DeckCount = deckCount;
            TotalCards = totalCards;
        }
    }
}
