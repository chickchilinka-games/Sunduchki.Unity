using System;
using System.Collections.Generic;
using System.Linq;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;
using Modules.PlayerHand.Model;
using R3;

namespace Modules.PlayerHand.Services
{
    public class PlayerHandService : IPlayerHandStateWriter
    {
        private readonly PlayerHandModel _model;
        private readonly Subject<BonusCardUsageEvent> _bonusUsed = new();
        private readonly Subject<StandardCardDrawnEvent> _standardCardDrawn = new();
        private readonly Subject<BonusCardDrawnEvent> _bonusCardDrawn = new();
        private readonly Subject<CardsReceivedEvent> _cardsReceived = new();

        public PlayerHandService(
            PlayerHandModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ReadOnlyReactiveProperty<PlayerHandState> ObserveHand(string playerId)
        {
            return _model.GetOrCreate(playerId);
        }

        public Observable<BonusCardUsageEvent> BonusUsed => _bonusUsed;
        public Observable<StandardCardDrawnEvent> StandardCardDrawn => _standardCardDrawn;
        public Observable<BonusCardDrawnEvent> BonusCardDrawn => _bonusCardDrawn;
        public Observable<CardsReceivedEvent> CardsReceived => _cardsReceived;

        public void ApplyStandardSnapshot(string playerId, IReadOnlyList<StandardCardData> cards)
        {
            Update(playerId, _ => new PlayerHandState(
                playerId,
                CopyStandard(cards),
                _model.GetOrCreate(playerId).Value.BonusCards));
        }

        public void AddStandardCard(string playerId, StandardCardData card)
        {
            var property = _model.GetOrCreate(playerId);
            var current = property.Value;
            if (current.StandardCards.Any(item => SameStandard(item, card)))
            {
                return;
            }

            _standardCardDrawn.OnNext(new StandardCardDrawnEvent(playerId, card));
            var list = current.StandardCards.ToList();
            list.Add(card);
            property.Value = new PlayerHandState(current.PlayerId, list, current.BonusCards);
        }

        public void RemoveStandardCard(string playerId, StandardCardData card)
        {
            Update(playerId, state =>
            {
                var list = state.StandardCards.ToList();
                var index = list.FindIndex(c => SameStandard(c, card));
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }

                return new PlayerHandState(state.PlayerId, list, state.BonusCards);
            });
        }

        public void AddBonusCard(string playerId, BonusCardData card)
        {
            Update(playerId, state =>
            {
                var list = state.BonusCards.ToList();
                list.Add(card);
                return new PlayerHandState(state.PlayerId, state.StandardCards, list);
            });

            _bonusCardDrawn.OnNext(new BonusCardDrawnEvent(playerId, card));
        }

        public void RemoveBonusCard(string playerId, BonusCardData card)
        {
            Update(playerId, state =>
            {
                var list = state.BonusCards.ToList();
                var index = list.FindIndex(c => SameBonus(c, card));
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }

                return new PlayerHandState(state.PlayerId, state.StandardCards, list);
            });
        }

        public void NotifyBonusUsed(string playerId, BonusCardData card)
        {
            _bonusUsed.OnNext(new BonusCardUsageEvent(playerId, card.BonusType));
        }

        public void NotifyCardsReceived(
            string playerId,
            string source,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards)
        {
            _cardsReceived.OnNext(new CardsReceivedEvent(playerId, source, standardCards, bonusCards));
        }

        public void ClearHand(string playerId)
        {
            Update(playerId, _ => PlayerHandState.Empty(playerId));
        }

        public void ResetAll()
        {
            _model.ResetAll();
        }

        private void Update(string playerId, Func<PlayerHandState, PlayerHandState> mutator)
        {
            var property = _model.GetOrCreate(playerId);
            property.Value = mutator(property.Value);
        }

        private static IReadOnlyList<StandardCardData> CopyStandard(IEnumerable<StandardCardData> source)
        {
            return source?.ToList() ?? new List<StandardCardData>();
        }

        private static bool SameStandard(StandardCardData left, StandardCardData right)
        {
            return string.Equals(left.Rank, right.Rank, StringComparison.Ordinal) &&
                   string.Equals(left.Suit, right.Suit, StringComparison.Ordinal);
        }

        private static bool SameBonus(BonusCardData left, BonusCardData right)
        {
            return string.Equals(left.BonusType, right.BonusType, StringComparison.Ordinal);
        }
    }
}
