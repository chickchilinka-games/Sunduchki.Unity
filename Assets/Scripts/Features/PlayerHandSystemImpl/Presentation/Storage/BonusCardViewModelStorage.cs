using System;
using System.Collections.Generic;
using System.Linq;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;

namespace Features.PlayerHandSystemImpl.Presentation.Storage
{
    public sealed class BonusCardViewModelStorage : IDisposable
    {
        private readonly ObservableList<BonusCardViewModel> _items = new();
        private readonly Dictionary<string, List<BonusCardViewModel>> _byType =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Subject<Unit> _changed = new();

        public IReadOnlyObservableList<BonusCardViewModel> Items => _items;
        public Observable<Unit> Changed => _changed;

        public void Sync(IReadOnlyList<BonusCardData> cards)
        {
            var incoming = cards ?? Array.Empty<BonusCardData>();
            var seenTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in incoming.GroupBy(card => NormalizeType(card.BonusType)))
            {
                var typeKey = group.Key;
                var count = Math.Max(0, group.Count());

                if (!_byType.TryGetValue(typeKey, out var viewModels))
                {
                    viewModels = new List<BonusCardViewModel>();
                    _byType[typeKey] = viewModels;
                }

                while (viewModels.Count < count)
                {
                    var vm = new BonusCardViewModel(typeKey);
                    viewModels.Add(vm);
                    _items.Add(vm);
                }

                while (viewModels.Count > count)
                {
                    var idx = viewModels.Count - 1;
                    var vm = viewModels[idx];
                    viewModels.RemoveAt(idx);
                    _items.Remove(vm);
                    vm.Dispose();
                }

                seenTypes.Add(typeKey);
            }

            var staleTypes = _byType.Keys.Where(type => !seenTypes.Contains(type)).ToList();
            foreach (var type in staleTypes)
            {
                if (_byType.Remove(type, out var viewModels))
                {
                    foreach (var vm in viewModels)
                    {
                        _items.Remove(vm);
                        vm.Dispose();
                    }
                }
            }

            _changed.OnNext(Unit.Default);
        }

        public void Clear()
        {
            foreach (var viewModel in _items)
            {
                viewModel.Dispose();
            }

            _items.Clear();
            _byType.Clear();
            _changed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            Clear();
            _changed.Dispose();
        }

        private static string NormalizeType(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType)
                ? "unknown-bonus"
                : bonusType.Trim();
        }
    }
}
