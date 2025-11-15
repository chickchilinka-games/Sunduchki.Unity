using System.Collections.Generic;

namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandState
    {
        public string PlayerId { get; }
        public IReadOnlyList<StandardCardData> StandardCards { get; }
        public IReadOnlyList<BonusCardData> BonusCards { get; }

        public static PlayerHandState Empty(string playerId) =>
            new PlayerHandState(playerId ?? string.Empty, new List<StandardCardData>(), new List<BonusCardData>());

        public PlayerHandState(
            string playerId,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards)
        {
            PlayerId = playerId ?? string.Empty;
            StandardCards = standardCards ?? new List<StandardCardData>();
            BonusCards = bonusCards ?? new List<BonusCardData>();
        }
    }
}
