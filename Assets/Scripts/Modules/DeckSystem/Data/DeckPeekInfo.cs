namespace Modules.DeckSystem.Data
{
    public readonly struct DeckPeekInfo
    {
        public static DeckPeekInfo None { get; } = new DeckPeekInfo(false, default, string.Empty, string.Empty);

        public bool HasValue { get; }
        public DeckCardData Card { get; }
        public string PlayerId { get; }
        public string BonusType { get; }

        private DeckPeekInfo(bool hasValue, DeckCardData card, string playerId, string bonusType)
        {
            HasValue = hasValue;
            Card = card;
            PlayerId = playerId ?? string.Empty;
            BonusType = bonusType ?? string.Empty;
        }

        public static DeckPeekInfo Create(DeckCardData card, string playerId, string bonusType)
        {
            return new DeckPeekInfo(true, card, playerId, bonusType);
        }
    }
}
