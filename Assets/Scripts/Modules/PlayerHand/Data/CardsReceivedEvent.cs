using System.Collections.Generic;

namespace Modules.PlayerHand.Data
{
    public readonly struct CardsReceivedEvent
    {
        public string PlayerId { get; }
        public string Source { get; }
        public IReadOnlyList<StandardCardData> StandardCards { get; }
        public IReadOnlyList<BonusCardData> BonusCards { get; }
        public long EventSeq { get; }
        public bool CompletedSet { get; }
        public string CompletedSetRank { get; }

        public CardsReceivedEvent(
            string playerId,
            string source,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards,
            long eventSeq = 0,
            bool completedSet = false,
            string completedSetRank = "")
        {
            PlayerId = playerId ?? string.Empty;
            Source = source ?? string.Empty;
            StandardCards = standardCards ?? new List<StandardCardData>();
            BonusCards = bonusCards ?? new List<BonusCardData>();
            EventSeq = eventSeq;
            CompletedSet = completedSet;
            CompletedSetRank = completedSetRank ?? string.Empty;
        }
    }
}
