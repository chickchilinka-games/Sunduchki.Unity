using System;
using System.Collections.Generic;
using System.Linq;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;
using Modules.PlayerHand.Model;
using R3;

namespace Modules.PlayerHand.Services
{
    public class PlayerHandService : IPlayerHandService, IPlayerHandStateWriter
    {
        private readonly PlayerHandModel _model;

        public PlayerHandService(
            PlayerHandModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ReadOnlyReactiveProperty<PlayerHandState> ObserveHand(string playerId)
        {
            return _model.GetOrCreate(playerId);
        }

        public void ApplyStandardSnapshot(string playerId, IReadOnlyList<StandardCardData> cards)
        {
            Update(playerId, _ => new PlayerHandState(
                playerId,
                CopyStandard(cards),
                _model.GetOrCreate(playerId).Value.BonusCards));
        }

        public void AddStandardCard(string playerId, StandardCardData card)
        {
            Update(playerId, state =>
            {
                var list = state.StandardCards.ToList();
                list.Add(card);
                return new PlayerHandState(state.PlayerId, list, state.BonusCards);
            });
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
