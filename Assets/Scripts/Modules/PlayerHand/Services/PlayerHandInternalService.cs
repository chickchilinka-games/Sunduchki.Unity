using System;
using System.Collections.Generic;
using System.Linq;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Model;

namespace Modules.PlayerHand.Services
{
    internal class PlayerHandInternalService
    {
        private readonly PlayerHandModel _model;
        private readonly Dictionary<string, long> _lastSnapshotRevision = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _lastEventSeq = new(StringComparer.Ordinal);

        public PlayerHandInternalService(PlayerHandModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void ApplyStandardSnapshot(string playerId, IReadOnlyList<StandardCardData> cards, long revision)
        {
            if (revision > 0)
            {
                if (_lastSnapshotRevision.TryGetValue(playerId, out var lastRevision) &&
                    revision <= lastRevision)
                {
                    return;
                }

                _lastSnapshotRevision[playerId] = revision;
            }

            Update(playerId, _ => new PlayerHandState(
                playerId,
                CopyStandard(cards),
                _model.GetOrCreate(playerId).Value.BonusCards));
        }

        public void AddStandardCard(string playerId, StandardCardData card, long eventSeq = 0)
        {
            if (IsStaleEvent(playerId, eventSeq))
            {
                return;
            }

            var property = _model.GetOrCreate(playerId);
            var current = property.Value;
            if (current.StandardCards.Any(item => SameStandard(item, card)))
            {
                return;
            }

            _model.PublishStandardCardDrawn(new StandardCardDrawnEvent(playerId, card));
            var list = current.StandardCards.ToList();
            list.Add(card);
            property.Value = new PlayerHandState(current.PlayerId, list, current.BonusCards);
        }

        public void RemoveStandardCard(
            string playerId,
            StandardCardData card,
            long eventSeq = 0,
            string completedSetRank = "")
        {
            if (IsStaleEvent(playerId, eventSeq))
            {
                return;
            }

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

            if (!string.IsNullOrWhiteSpace(completedSetRank))
            {
                ApplySetCompleted(playerId, completedSetRank, eventSeq);
            }
        }

        public void AddBonusCard(string playerId, BonusCardData card)
        {
            Update(playerId, state =>
            {
                var list = state.BonusCards.ToList();
                list.Add(card);
                return new PlayerHandState(state.PlayerId, state.StandardCards, list);
            });

            _model.PublishBonusCardDrawn(new BonusCardDrawnEvent(playerId, card));
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
            _model.PublishBonusUsed(new BonusCardUsageEvent(playerId, card.BonusType));
        }

        public void NotifyCardsReceived(
            string playerId,
            string source,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards,
            long eventSeq = 0,
            bool completedSet = false,
            string completedSetRank = "")
        {
            _model.PublishCardsReceived(new CardsReceivedEvent(
                playerId,
                source,
                standardCards,
                bonusCards,
                eventSeq,
                completedSet,
                completedSetRank));
        }

        public void ApplySetCompleted(string playerId, string rank, long eventSeq = 0)
        {
            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            if (IsStaleEvent(playerId, eventSeq))
            {
                return;
            }

            var normalizedRank = rank.Trim();
            Update(playerId, state =>
            {
                if (state.StandardCards.Count == 0)
                {
                    return state;
                }

                var updated = state.StandardCards
                    .Where(card => !string.Equals(card.Rank, normalizedRank, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return new PlayerHandState(state.PlayerId, updated, state.BonusCards);
            });
        }

        public void ClearHand(string playerId)
        {
            Update(playerId, _ => PlayerHandState.Empty(playerId));
        }

        public void ResetAll()
        {
            _lastSnapshotRevision.Clear();
            _lastEventSeq.Clear();
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

        private bool IsStaleEvent(string playerId, long eventSeq)
        {
            if (eventSeq <= 0)
            {
                return false;
            }

            if (!_lastEventSeq.TryGetValue(playerId, out var lastSeq))
            {
                _lastEventSeq[playerId] = eventSeq;
                return false;
            }

            if (eventSeq < lastSeq)
            {
                return true;
            }

            if (eventSeq > lastSeq)
            {
                _lastEventSeq[playerId] = eventSeq;
            }

            return false;
        }
    }
}
