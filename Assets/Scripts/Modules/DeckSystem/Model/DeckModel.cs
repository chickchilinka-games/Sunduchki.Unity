using System.Collections.Generic;
using Modules.DeckSystem.Data;
using R3;

namespace Modules.DeckSystem.Model
{
    public class DeckModel
    {
        private readonly ReactiveProperty<DeckState> _state = new(DeckState.Default);
        private readonly Subject<DeckConfiguredEvent> _configured = new();
        private readonly Subject<DeckCardDrawnEvent> _standardDrawn = new();
        private readonly Subject<DeckBonusCardDrawnEvent> _bonusDrawn = new();
        private readonly Subject<DeckPeekedEvent> _peeked = new();
        private readonly Subject<DeckAdjustedEvent> _adjusted = new();

        public ReadOnlyReactiveProperty<DeckState> State => _state;
        public Observable<DeckConfiguredEvent> Configured => _configured;
        public Observable<DeckCardDrawnEvent> StandardDrawn => _standardDrawn;
        public Observable<DeckBonusCardDrawnEvent> BonusDrawn => _bonusDrawn;
        public Observable<DeckPeekedEvent> Peeked => _peeked;
        public Observable<DeckAdjustedEvent> Adjusted => _adjusted;

        public DeckState Current => _state.Value;

        public void SetState(DeckState state)
        {
            _state.Value = state;
        }

        public void PublishConfigured(int? remainingCards, int? totalCards)
        {
            _configured.OnNext(new DeckConfiguredEvent(remainingCards, totalCards));
        }

        public void PublishStandardDrawn(string playerId, DeckCardData card)
        {
            _standardDrawn.OnNext(new DeckCardDrawnEvent(playerId, card));
        }

        public void PublishBonusDrawn(string playerId, string bonusType)
        {
            _bonusDrawn.OnNext(new DeckBonusCardDrawnEvent(playerId, bonusType));
        }

        public void PublishPeeked(string playerId, IReadOnlyList<DeckPeekCardData> cards)
        {
            _peeked.OnNext(new DeckPeekedEvent(playerId, cards));
        }

        public void PublishAdjusted(int delta)
        {
            _adjusted.OnNext(new DeckAdjustedEvent(delta));
        }
    }
}
