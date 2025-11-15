namespace Modules.PlayerHand.Data
{
    public readonly struct StandardCardData
    {
        public string Rank { get; }
        public string Suit { get; }

        public StandardCardData(string rank, string suit)
        {
            Rank = rank ?? string.Empty;
            Suit = suit ?? string.Empty;
        }
    }
}
