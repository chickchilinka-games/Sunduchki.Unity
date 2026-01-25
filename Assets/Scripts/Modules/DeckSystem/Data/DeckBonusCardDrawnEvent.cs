using System;

namespace Modules.DeckSystem.Data
{
    public readonly struct DeckBonusCardDrawnEvent
    {
        public string PlayerId { get; }
        public string BonusType { get; }

        public DeckBonusCardDrawnEvent(string playerId, string bonusType)
        {
            PlayerId = playerId ?? string.Empty;
            BonusType = bonusType ?? string.Empty;
        }
    }
}
