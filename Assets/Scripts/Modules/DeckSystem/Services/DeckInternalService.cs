using System;
using System.Collections.Generic;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Model;
using R3;

namespace Modules.DeckSystem.Services
{
    internal class DeckInternalService
    {
        private readonly DeckModel _model;

        public DeckInternalService(DeckModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public Observable<DeckCardDrawnEvent> StandardDrawn => _model.StandardDrawn;
        public Observable<DeckBonusCardDrawnEvent> BonusDrawn => _model.BonusDrawn;

        public void OnDeckConfigured(int? remainingCards, int? totalCards)
        {
            var current = _model.Current;
            var nextRemaining = remainingCards ?? current.RemainingCards;
            var nextTotal = totalCards ?? current.TotalCards;
            _model.SetState(current.WithCounts(nextRemaining, nextTotal).WithPeek(DeckPeekInfo.None));
            _model.PublishConfigured(remainingCards, totalCards);
        }

        public void OnStandardCardDrawn(string playerId, DeckCardData card)
        {
            _model.SetState(_model.Current.WithPeek(DeckPeekInfo.None));
            _model.PublishStandardDrawn(playerId, card);
        }

        public void OnBonusCardDrawn(string playerId, string bonusType)
        {
            _model.SetState(_model.Current.WithPeek(DeckPeekInfo.None));
            _model.PublishBonusDrawn(playerId, bonusType);
        }

        public void OnDeckPeeked(string playerId, IReadOnlyList<DeckPeekCardData> cards)
        {
            var peekInfo = DeckPeekInfo.Create(cards ?? Array.Empty<DeckPeekCardData>(), playerId);
            _model.SetState(_model.Current.WithPeek(peekInfo));
            _model.PublishPeeked(playerId, cards ?? Array.Empty<DeckPeekCardData>());
        }

        public void OnDeckAdjusted(int delta)
        {
            var current = _model.Current;
            if (delta != 0 && current.RemainingCards.HasValue)
            {
                var next = current.RemainingCards.Value + delta;
                if (next < 0)
                {
                    next = 0;
                }

                current = current.WithRemaining(next);
            }

            _model.SetState(current.WithPeek(DeckPeekInfo.None));
            _model.PublishAdjusted(delta);
        }

        public void ResetState()
        {
            _model.SetState(DeckState.Default);
        }
    }
}
