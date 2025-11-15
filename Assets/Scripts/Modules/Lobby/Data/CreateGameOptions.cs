namespace Modules.Lobby.Data
{
    public struct CreateGameOptions
    {
        public DeckType Deck { get; }
        public int StartingHand { get; }
        public string Mode { get; }

        public CreateGameOptions(DeckType deck, int startingHand, string mode)
        {
            Deck = deck;
            StartingHand = startingHand;
            Mode = mode;
        }
    }
}
