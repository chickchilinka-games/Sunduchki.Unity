namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandStandardCardEvent
    {
        public string PlayerId { get; }
        public StandardCardData Card { get; }
        public long EventSeq { get; }
        public bool CompletedSet { get; }
        public string CompletedSetRank { get; }

        public PlayerHandStandardCardEvent(
            string playerId,
            StandardCardData card,
            long eventSeq = 0,
            bool completedSet = false,
            string completedSetRank = "")
        {
            PlayerId = playerId ?? string.Empty;
            Card = card;
            EventSeq = eventSeq;
            CompletedSet = completedSet;
            CompletedSetRank = completedSetRank ?? string.Empty;
        }
    }
}
