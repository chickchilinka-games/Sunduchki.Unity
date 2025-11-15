namespace Modules.DeckSystem.Data
{
    public readonly struct DeckState
    {
        public static DeckState Default { get; } = new DeckState(null, null, DeckPeekInfo.None);

        public int? RemainingCards { get; }
        public int? TotalCards { get; }
        public DeckPeekInfo PeekInfo { get; }

        public DeckState(int? remainingCards, int? totalCards, DeckPeekInfo peekInfo)
        {
            RemainingCards = remainingCards;
            TotalCards = totalCards;
            PeekInfo = peekInfo;
        }

        public DeckState WithCounts(int? remainingCards, int? totalCards)
        {
            return new DeckState(remainingCards, totalCards, PeekInfo);
        }

        public DeckState WithRemaining(int? remainingCards)
        {
            return new DeckState(remainingCards, TotalCards, PeekInfo);
        }

        public DeckState WithPeek(DeckPeekInfo info)
        {
            return new DeckState(RemainingCards, TotalCards, info);
        }
    }
}
