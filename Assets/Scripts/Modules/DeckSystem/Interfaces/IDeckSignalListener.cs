using Modules.DeckSystem.Data;

namespace Modules.DeckSystem.Interfaces
{
    public interface IDeckSignalListener
    {
        void OnDeckConfigured(int? remainingCards, int? totalCards);

        void OnStandardCardDrawn(string playerId, DeckCardData card);

        void OnBonusCardDrawn(string playerId, string bonusType);

        void OnDeckPeeked(string playerId, DeckCardData card, string bonusType);
    }
}
