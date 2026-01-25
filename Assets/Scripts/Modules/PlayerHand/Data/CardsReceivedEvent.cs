using System.Collections.Generic;

namespace Modules.PlayerHand.Data
{
    public readonly struct CardsReceivedEvent
    {
        public string PlayerId { get; }
        public string Source { get; }
        public IReadOnlyList<StandardCardData> StandardCards { get; }
        public IReadOnlyList<BonusCardData> BonusCards { get; }

        public CardsReceivedEvent(
            string playerId,
            string source,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards)
        {
            PlayerId = playerId ?? string.Empty;
            Source = source ?? string.Empty;
            StandardCards = standardCards ?? new List<StandardCardData>();
            BonusCards = bonusCards ?? new List<BonusCardData>();
        }
    }
}
