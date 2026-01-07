namespace Modules.DeckSystem.Data
{
    public readonly struct DeckPeekCardData
    {
        public string Rank { get; }
        public string Suit { get; }
        public string BonusType { get; }

        public DeckPeekCardData(string rank, string suit, string bonusType)
        {
            Rank = rank ?? string.Empty;
            Suit = suit ?? string.Empty;
            BonusType = bonusType ?? string.Empty;
        }

        public bool IsBonus => !string.IsNullOrWhiteSpace(BonusType);
    }
}
