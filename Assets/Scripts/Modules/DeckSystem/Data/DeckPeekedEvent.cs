using System.Collections.Generic;

namespace Modules.DeckSystem.Data
{
    public readonly struct DeckPeekedEvent
    {
        public string PlayerId { get; }
        public IReadOnlyList<DeckPeekCardData> Cards { get; }

        public DeckPeekedEvent(string playerId, IReadOnlyList<DeckPeekCardData> cards)
        {
            PlayerId = playerId ?? string.Empty;
            Cards = cards ?? new List<DeckPeekCardData>();
        }
    }
}
