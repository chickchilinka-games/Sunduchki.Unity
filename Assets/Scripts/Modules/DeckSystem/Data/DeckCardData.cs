namespace Modules.DeckSystem.Data
{
    public readonly struct DeckCardData
    {
        public string Rank { get; }
        public string Suit { get; }

        public DeckCardData(string rank, string suit)
        {
            Rank = rank ?? string.Empty;
            Suit = suit ?? string.Empty;
        }
    }
}
