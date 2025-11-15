namespace Modules.CardRequestSystem.Data
{
    public readonly struct CardRequestState
    {
        public static CardRequestState Empty { get; } = new CardRequestState(0, false, default);

        public int Sequence { get; }
        public bool HasEvent { get; }
        public CardRequestEvent LastEvent { get; }

        private CardRequestState(int sequence, bool hasEvent, CardRequestEvent lastEvent)
        {
            Sequence = sequence;
            HasEvent = hasEvent;
            LastEvent = lastEvent;
        }

        public CardRequestState Next(CardRequestEvent nextEvent)
        {
            return new CardRequestState(Sequence + 1, true, nextEvent);
        }
    }
}
