using System.Collections.Generic;

namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandDeltaEvent
    {
        public string ActionId { get; }
        public string PlayerId { get; }
        public string Source { get; }
        public string Destination { get; }
        public IReadOnlyList<StandardCardData> AddedStandardCards { get; }
        public IReadOnlyList<BonusCardData> AddedBonusCards { get; }
        public IReadOnlyList<StandardCardData> RemovedStandardCards { get; }
        public IReadOnlyList<BonusCardData> RemovedBonusCards { get; }
        public long EventSeq { get; }
        public bool CompletedSet { get; }
        public string CompletedSetRank { get; }

        public PlayerHandDeltaEvent(
            string actionId,
            string playerId,
            string source,
            string destination,
            IReadOnlyList<StandardCardData> addedStandardCards,
            IReadOnlyList<BonusCardData> addedBonusCards,
            IReadOnlyList<StandardCardData> removedStandardCards,
            IReadOnlyList<BonusCardData> removedBonusCards,
            long eventSeq = 0,
            bool completedSet = false,
            string completedSetRank = "")
        {
            ActionId = actionId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
            Source = source ?? string.Empty;
            Destination = destination ?? string.Empty;
            AddedStandardCards = addedStandardCards ?? new List<StandardCardData>();
            AddedBonusCards = addedBonusCards ?? new List<BonusCardData>();
            RemovedStandardCards = removedStandardCards ?? new List<StandardCardData>();
            RemovedBonusCards = removedBonusCards ?? new List<BonusCardData>();
            EventSeq = eventSeq;
            CompletedSet = completedSet;
            CompletedSetRank = completedSetRank ?? string.Empty;
        }
    }
}
