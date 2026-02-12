namespace Modules.DeckSystem.Data
{
    public readonly struct DeckConfiguredEvent
    {
        public int? RemainingCards { get; }
        public int? TotalCards { get; }

        public DeckConfiguredEvent(int? remainingCards, int? totalCards)
        {
            RemainingCards = remainingCards;
            TotalCards = totalCards;
        }
    }
}
