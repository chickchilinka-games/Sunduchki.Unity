using System;

namespace Modules.DeckSystem.Data
{
    public readonly struct DeckCardDrawnEvent
    {
        public string PlayerId { get; }
        public DeckCardData Card { get; }

        public DeckCardDrawnEvent(string playerId, DeckCardData card)
        {
            PlayerId = playerId ?? string.Empty;
            Card = card;
        }
    }
}
