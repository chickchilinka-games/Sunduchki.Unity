using System;
using System.Collections.Generic;
using System.Linq;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;

namespace Features.PlayerHandSystemImpl.Presentation.Storage
{
    public sealed class RankStackViewModelStorage : IDisposable
    {
        private readonly IRankStackViewModelFactory _factory;
        private readonly ObservableList<RankStackViewModel> _items = new();
        private readonly Dictionary<string, RankStackViewModel> _byRank =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Subject<Unit> _changed = new();
        private long _snapshotRevision;

        public RankStackViewModelStorage(IRankStackViewModelFactory factory)
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

            foreach (var group in incoming.GroupBy(card => NormalizeRank(card.Rank)))
            {
                var rankKey = group.Key;
                var suits = group.Select(card => NormalizeSuit(card.Suit)).ToArray();

                if (!_byRank.TryGetValue(rankKey, out var viewModel))
                {
                    viewModel = _factory.Create(rankKey, suits);
                    _byRank[rankKey] = viewModel;
                    _items.Add(viewModel);
                }
                else
                {
                    viewModel.ApplySnapshot(suits, NextSnapshotRevision());
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
            _snapshotRevision = 0;
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

        private long NextSnapshotRevision()
        {
            _snapshotRevision++;
            return _snapshotRevision;
        }

    }
}
