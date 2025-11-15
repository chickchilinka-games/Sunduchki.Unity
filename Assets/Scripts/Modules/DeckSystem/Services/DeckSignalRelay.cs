using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;

namespace Modules.DeckSystem.Services
{
    public class DeckSignalRelay : IDeckSignalHandler
    {
        private readonly IDeckStateWriter _stateWriter;

        public DeckSignalRelay(IDeckStateWriter stateWriter)
        {
            _stateWriter = stateWriter;
        }

        public void OnDeckConfigured(int? remainingCards, int? totalCards)
        {
            _stateWriter.ConfigureCounts(remainingCards, totalCards);
            _stateWriter.ClearPeek();
        }

        public void OnStandardCardDrawn(string playerId, DeckCardData card)
        {
            _stateWriter.AdjustBy(-1);
            _stateWriter.ClearPeek();
        }

        public void OnBonusCardDrawn(string playerId, string bonusType)
        {
            _stateWriter.AdjustBy(-1);
            _stateWriter.ClearPeek();
        }

        public void OnDeckPeeked(string playerId, DeckCardData card, string bonusType)
        {
            _stateWriter.SetPeek(DeckPeekInfo.Create(card, playerId, bonusType));
        }

        public void ResetState()
        {
            _stateWriter.Reset();
        }
    }
}
