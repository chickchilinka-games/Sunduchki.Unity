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

        public void ApplySnapshot(
            string playerId,
            IReadOnlyList<StandardCardData> standardCards,
            IReadOnlyList<BonusCardData> bonusCards,
            long revision)
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
                CopyStandard(standardCards),
                CopyBonus(bonusCards)));
        }

        public void ApplyDelta(PlayerHandDeltaEvent delta, string resolvedPlayerId)
        {
            var playerId = string.IsNullOrWhiteSpace(resolvedPlayerId)
                ? delta.PlayerId
                : resolvedPlayerId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            if (IsStaleEvent(playerId, delta.EventSeq))
            {
                return;
            }

            if (delta.RemovedStandardCards.Count > 0)
            {
                RemoveStandardCards(playerId, delta.RemovedStandardCards);
            }

            if (delta.RemovedBonusCards.Count > 0)
            {
                RemoveBonusCards(playerId, delta.RemovedBonusCards);
            }

            if (delta.AddedStandardCards.Count > 0)
            {
                AddStandardCards(playerId, delta.AddedStandardCards);
            }

            if (delta.AddedBonusCards.Count > 0)
            {
                AddBonusCards(playerId, delta.AddedBonusCards);
            }

            if (delta.AddedStandardCards.Count > 0 || delta.AddedBonusCards.Count > 0)
            {
                _model.PublishCardsReceived(new CardsReceivedEvent(
                    playerId,
                    delta.Source,
                    delta.AddedStandardCards,
                    delta.AddedBonusCards,
                    delta.EventSeq,
                    delta.CompletedSet,
                    delta.CompletedSetRank));
            }

            if (delta.CompletedSet && !string.IsNullOrWhiteSpace(delta.CompletedSetRank))
            {
                ApplySetCompleted(playerId, delta.CompletedSetRank, delta.EventSeq);
            }
        }

        public void ResetAll()
        {
            _lastSnapshotRevision.Clear();
            _lastEventSeq.Clear();
            _model.ResetAll();
        }

        private void AddStandardCards(string playerId, IReadOnlyList<StandardCardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return;
            }

            Update(playerId, state =>
            {
                var list = state.StandardCards.ToList();
                foreach (var card in cards)
                {
                    if (list.Any(item => SameStandard(item, card)))
                    {
                        continue;
                    }

                    _model.PublishStandardCardDrawn(new StandardCardDrawnEvent(playerId, card));
                    list.Add(card);
                }

                return new PlayerHandState(state.PlayerId, list, state.BonusCards);
            });
        }

        private void RemoveStandardCards(string playerId, IReadOnlyList<StandardCardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return;
            }

            Update(playerId, state =>
            {
                var list = state.StandardCards.ToList();
                foreach (var card in cards)
                {
                    var index = list.FindIndex(item => SameStandard(item, card));
                    if (index >= 0)
                    {
                        list.RemoveAt(index);
                    }
                }

                return new PlayerHandState(state.PlayerId, list, state.BonusCards);
            });
        }

        private void AddBonusCards(string playerId, IReadOnlyList<BonusCardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return;
            }

            Update(playerId, state =>
            {
                var list = state.BonusCards.ToList();
                foreach (var card in cards)
                {
                    list.Add(card);
                    _model.PublishBonusCardDrawn(new BonusCardDrawnEvent(playerId, card));
                }

                return new PlayerHandState(state.PlayerId, state.StandardCards, list);
            });
        }

        private void RemoveBonusCards(string playerId, IReadOnlyList<BonusCardData> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                return;
            }

            Update(playerId, state =>
            {
                var list = state.BonusCards.ToList();
                foreach (var card in cards)
                {
                    var index = list.FindIndex(item => SameBonus(item, card));
                    if (index >= 0)
                    {
                        list.RemoveAt(index);
                        _model.PublishBonusUsed(new BonusCardUsageEvent(playerId, card.BonusType));
                    }
                }

                return new PlayerHandState(state.PlayerId, state.StandardCards, list);
            });
        }

        private void ApplySetCompleted(string playerId, string rank, long eventSeq = 0)
        {
            if (string.IsNullOrWhiteSpace(rank))
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

        private void Update(string playerId, Func<PlayerHandState, PlayerHandState> mutator)
        {
            var property = _model.GetOrCreate(playerId);
            property.Value = mutator(property.Value);
        }

        private static IReadOnlyList<StandardCardData> CopyStandard(IEnumerable<StandardCardData> source)
        {
            return source?.ToList() ?? new List<StandardCardData>();
        }

        private static IReadOnlyList<BonusCardData> CopyBonus(IEnumerable<BonusCardData> source)
        {
            return source?.ToList() ?? new List<BonusCardData>();
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
