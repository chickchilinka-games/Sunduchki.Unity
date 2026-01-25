using System;
using System.Collections.Generic;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;
using R3;

namespace Modules.DeckSystem.Services
{
    public class DeckSignalRelay : IDeckSignalHandler, IDeckDrawEventStream
    {
        private readonly IDeckStateWriter _stateWriter;
        private readonly Subject<DeckCardDrawnEvent> _standardDrawn = new();
        private readonly Subject<DeckBonusCardDrawnEvent> _bonusDrawn = new();

        public DeckSignalRelay(IDeckStateWriter stateWriter)
        {
            _stateWriter = stateWriter;
        }

        public Observable<DeckCardDrawnEvent> StandardDrawn => _standardDrawn;
        public Observable<DeckBonusCardDrawnEvent> BonusDrawn => _bonusDrawn;

        public void OnDeckConfigured(int? remainingCards, int? totalCards)
        {
            _stateWriter.ConfigureCounts(remainingCards, totalCards);
            _stateWriter.ClearPeek();
        }

        public void OnStandardCardDrawn(string playerId, DeckCardData card)
        {
            _stateWriter.ClearPeek();
            _standardDrawn.OnNext(new DeckCardDrawnEvent(playerId, card));
        }

        public void OnBonusCardDrawn(string playerId, string bonusType)
        {
            _stateWriter.ClearPeek();
            _bonusDrawn.OnNext(new DeckBonusCardDrawnEvent(playerId, bonusType));
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
