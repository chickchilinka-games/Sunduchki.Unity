using System.Collections.Generic;
using Modules.DeckSystem.Data;

namespace Modules.DeckSystem.Interfaces
{
    public interface IDeckSignalListener
    {
        void OnDeckConfigured(int? remainingCards, int? totalCards);

        void OnStandardCardDrawn(string playerId, DeckCardData card);

        void OnBonusCardDrawn(string playerId, string bonusType);

        void OnDeckPeeked(string playerId, IReadOnlyList<DeckPeekCardData> cards);

        void OnDeckAdjusted(int delta);
    }
}
