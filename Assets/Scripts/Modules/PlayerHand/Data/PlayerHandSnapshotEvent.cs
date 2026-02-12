using System.Collections.Generic;

namespace Modules.PlayerHand.Data
{
    public readonly struct PlayerHandSnapshotEvent
    {
        public string PlayerId { get; }
        public IReadOnlyList<StandardCardData> Cards { get; }

        public PlayerHandSnapshotEvent(string playerId, IReadOnlyList<StandardCardData> cards)
        {
            PlayerId = playerId ?? string.Empty;
            Cards = cards ?? new List<StandardCardData>();
        }
    }
}
