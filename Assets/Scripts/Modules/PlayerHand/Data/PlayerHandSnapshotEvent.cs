using System.Collections.Generic;

namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandSnapshotEvent
    {
        public string PlayerId { get; }
        public IReadOnlyList<StandardCardData> StandardCards { get; }
        public IReadOnlyList<BonusCardData> BonusCards { get; }
        public long Revision { get; }

        public PlayerHandSnapshotEvent(
            string playerId,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards,
            long revision)
        {
            PlayerId = playerId ?? string.Empty;
            StandardCards = standardCards ?? new List<StandardCardData>();
            BonusCards = bonusCards ?? new List<BonusCardData>();
            Revision = revision;
        }
    }
}
