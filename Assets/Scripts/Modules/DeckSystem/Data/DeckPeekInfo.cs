using System;
using System.Collections.Generic;

namespace Modules.DeckSystem.Data
{
    public readonly struct DeckPeekInfo
    {
        public static DeckPeekInfo None { get; } = new DeckPeekInfo(false, Array.Empty<DeckPeekCardData>(), string.Empty);

        public bool HasValue { get; }
        public string PlayerId { get; }
        public IReadOnlyList<DeckPeekCardData> Cards { get; }

        private DeckPeekInfo(bool hasValue, IReadOnlyList<DeckPeekCardData> cards, string playerId)
        {
            HasValue = hasValue;
            PlayerId = playerId ?? string.Empty;
            Cards = cards ?? Array.Empty<DeckPeekCardData>();
        }

        public static DeckPeekInfo Create(IReadOnlyList<DeckPeekCardData> cards, string playerId)
        {
            return new DeckPeekInfo(true, cards, playerId);
        }
    }
}
