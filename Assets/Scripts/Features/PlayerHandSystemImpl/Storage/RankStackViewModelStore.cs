using System;
using System.Collections.Generic;
using System.Linq;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;

namespace Features.PlayerHandSystemImpl.Storage
{
    public sealed class RankStackViewModelStore : IDisposable
    {
        private readonly IRankStackViewModelFactory _factory;
        private readonly ObservableList<RankStackViewModel> _items = new();
        private readonly Dictionary<string, RankStackViewModel> _byRank =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _suppressedUntilByRank =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Subject<Unit> _changed = new();

        public RankStackViewModelStore(IRankStackViewModelFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public IReadOnlyObservableList<RankStackViewModel> Items => _items;
        public Observable<Unit> Changed => _changed;

        public bool TryGet(string rank, out RankStackViewModel viewModel)
        {
            viewModel = null;
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            return _byRank.TryGetValue(rank, out viewModel);
        }

        public void Sync(IReadOnlyList<StandardCardData> cards)
        {
            var incoming = cards ?? Array.Empty<StandardCardData>();
            var seenRanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            PruneExpiredSuppressions();

            foreach (var group in incoming.GroupBy(card => NormalizeRank(card.Rank)))
            {
                var rankKey = group.Key;
                if (IsSuppressed(rankKey))
                {
                    continue;
                }

                var suits = group.Select(card => NormalizeSuit(card.Suit)).ToArray();

                if (!_byRank.TryGetValue(rankKey, out var viewModel))
                {
                    viewModel = _factory.Create(rankKey, suits);
                    _byRank[rankKey] = viewModel;
                    _items.Add(viewModel);
                }
                else
                {
                    viewModel.ApplySnapshot(suits);
                }

                seenRanks.Add(rankKey);
            }

            var staleRanks = _byRank.Keys.Where(rank => !seenRanks.Contains(rank)).ToList();
            foreach (var rank in staleRanks)
            {
                if (_byRank.Remove(rank, out var viewModel))
                {
                    _items.Remove(viewModel);
                    viewModel.Dispose();
                }
            }

            _changed.OnNext(Unit.Default);
        }

        public void SuppressRank(string rank, TimeSpan duration)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            if (duration <= TimeSpan.Zero)
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            _suppressedUntilByRank[rankKey] = DateTime.UtcNow + duration;
        }

        public bool Remove(string rank, out RankStackViewModel removed)
        {
            removed = null;
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            var rankKey = NormalizeRank(rank);
            if (!_byRank.Remove(rankKey, out removed))
            {
                return false;
            }

            _items.Remove(removed);
            removed.Dispose();
            _changed.OnNext(Unit.Default);
            return true;
        }

        public void Clear()
        {
            foreach (var viewModel in _items)
            {
                viewModel.Dispose();
            }

            _items.Clear();
            _byRank.Clear();
            _suppressedUntilByRank.Clear();
            _changed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            Clear();
            _changed.Dispose();
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank)
                ? "unknown"
                : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit)
                ? string.Empty
                : suit.Trim().ToLowerInvariant();
        }

        private bool IsSuppressed(string rankKey)
        {
            if (!_suppressedUntilByRank.TryGetValue(rankKey, out var until))
            {
                return false;
            }

            if (until > DateTime.UtcNow)
            {
                return true;
            }

            _suppressedUntilByRank.Remove(rankKey);
            return false;
        }

        private void PruneExpiredSuppressions()
        {
            if (_suppressedUntilByRank.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var expired = _suppressedUntilByRank
                .Where(pair => pair.Value <= now)
                .Select(pair => pair.Key)
                .ToList();
            foreach (var key in expired)
            {
                _suppressedUntilByRank.Remove(key);
            }
        }
    }
}
