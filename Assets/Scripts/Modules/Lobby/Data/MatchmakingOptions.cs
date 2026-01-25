namespace Modules.Lobby.Data
{
    public readonly struct MatchmakingOptions
    {
        public DeckType Deck { get; }
        public int StartingHand { get; }
        public string Mode { get; }

        public MatchmakingOptions(DeckType deck, int startingHand, string mode)
        {
            Deck = deck;
            StartingHand = startingHand;
            Mode = mode;
        }
    }
}
