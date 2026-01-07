using System;
using System.Collections.Generic;
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
            _stateWriter.ClearPeek();
        }

        public void OnBonusCardDrawn(string playerId, string bonusType)
        {
            _stateWriter.ClearPeek();
        }

        public void OnDeckPeeked(string playerId, IReadOnlyList<DeckPeekCardData> cards)
        {
            _stateWriter.SetPeek(DeckPeekInfo.Create(cards ?? Array.Empty<DeckPeekCardData>(), playerId));
        }

        public void OnDeckAdjusted(int delta)
        {
            _stateWriter.AdjustBy(delta);
            _stateWriter.ClearPeek();
        }

        public void ResetState()
        {
            _stateWriter.Reset();
        }
    }
}
