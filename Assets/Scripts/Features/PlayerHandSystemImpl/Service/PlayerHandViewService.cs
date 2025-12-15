using System;
using System.Collections.Generic;
using System.Linq;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.Lobby.Services;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Services;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.Service
{
    /// <summary>
    /// Stateless factory that produces runtime bindings for the player hand view.
    /// </summary>
    public class PlayerHandViewService
    {
        private readonly PlayerHandService _playerHandService;
        private readonly LobbyService _lobbyService;

        public PlayerHandViewService(PlayerHandService playerHandService, LobbyService lobbyService)
        {
            _playerHandService = playerHandService ?? throw new ArgumentNullException(nameof(playerHandService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        public PlayerHandViewRuntime CreateRuntime()
        {
            return new PlayerHandViewRuntime(_playerHandService, _lobbyService);
        }
    }

    /// <summary>
    /// Stateful runtime that keeps track of view-models for the hand.
    /// </summary>
    public sealed class PlayerHandViewRuntime : IDisposable
    {
        private readonly PlayerHandService _playerHandService;
        private readonly LobbyService _lobbyService;
        private readonly ObservableList<StandardCardViewModel> _standardCards = new();
        private readonly ObservableList<BonusCardViewModel> _bonusCards = new();
        private readonly Dictionary<string, StandardCardViewModel> _standardByRank = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<BonusCardViewModel>> _bonusesByType = new(StringComparer.OrdinalIgnoreCase);
        private readonly Subject<Unit> _handChanged = new();

        private IDisposable _handSubscription;

        public IReadOnlyObservableList<StandardCardViewModel> StandardCards => _standardCards;
        public IReadOnlyObservableList<BonusCardViewModel> BonusCards => _bonusCards;
        public Observable<Unit> HandChanged => _handChanged;

        public PlayerHandViewRuntime(PlayerHandService playerHandService, LobbyService lobbyService)
        {
            _playerHandService = playerHandService ?? throw new ArgumentNullException(nameof(playerHandService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        public bool Start()
        {
            if (_handSubscription != null)
            {
                return true;
            }

            var localPlayerId = _lobbyService.GetLocalPlayer().Id;
            if (string.IsNullOrWhiteSpace(localPlayerId))
            {
                Debug.unityLogger.LogWarning("PlayerHand", "Cannot start PlayerHandViewRuntime without a local player id.");
                return false;
            }

            _handSubscription = _playerHandService
                .ObserveHand(localPlayerId)
                .Subscribe(UpdateHand);
            return true;
        }

        public void Stop()
        {
            _handSubscription?.Dispose();
            _handSubscription = null;
            ClearViewModels();
        }

        public void Dispose()
        {
            Stop();
        }

        private void UpdateHand(PlayerHandState state)
        {
            SyncStandardCards(state.StandardCards);
            SyncBonusCards(state.BonusCards);
            _handChanged.OnNext(Unit.Default);
        }

        private void SyncStandardCards(IReadOnlyList<StandardCardData> cards)
        {
            var incoming = cards ?? Array.Empty<StandardCardData>();
            var seenRanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in incoming.GroupBy(card => NormalizeRank(card.Rank)))
            {
                var rankKey = group.Key;
                var suits = group.Select(card => NormalizeSuit(card.Suit)).ToArray();

                if (!_standardByRank.TryGetValue(rankKey, out var viewModel))
                {
                    viewModel = new StandardCardViewModel(rankKey, suits);
                    _standardByRank[rankKey] = viewModel;
                    _standardCards.Add(viewModel);
                }
                else
                {
                    viewModel.ApplySnapshot(suits);
                }

                seenRanks.Add(rankKey);
            }

            var staleRanks = _standardByRank.Keys.Where(rank => !seenRanks.Contains(rank)).ToList();
            foreach (var rank in staleRanks)
            {
                if (_standardByRank.Remove(rank, out var viewModel))
                {
                    _standardCards.Remove(viewModel);
                    viewModel.Dispose();
                }
            }
        }

        private void SyncBonusCards(IReadOnlyList<BonusCardData> cards)
        {
            var incoming = cards ?? Array.Empty<BonusCardData>();
            var seenTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in incoming.GroupBy(card => NormalizeType(card.BonusType)))
            {
                var typeKey = group.Key;
                var count = Mathf.Max(0, group.Count());

                if (!_bonusesByType.TryGetValue(typeKey, out var viewModels))
                {
                    viewModels = new List<BonusCardViewModel>();
                    _bonusesByType[typeKey] = viewModels;
                }

                while (viewModels.Count < count)
                {
                    var vm = new BonusCardViewModel(typeKey);
                    viewModels.Add(vm);
                    _bonusCards.Add(vm);
                }

                while (viewModels.Count > count)
                {
                    var idx = viewModels.Count - 1;
                    var vm = viewModels[idx];
                    viewModels.RemoveAt(idx);
                    _bonusCards.Remove(vm);
                    vm.Dispose();
                }

                seenTypes.Add(typeKey);
            }

            var staleTypes = _bonusesByType.Keys.Where(type => !seenTypes.Contains(type)).ToList();
            foreach (var type in staleTypes)
            {
                if (_bonusesByType.Remove(type, out var viewModels))
                {
                    foreach (var vm in viewModels)
                    {
                        _bonusCards.Remove(vm);
                        vm.Dispose();
                    }
                }
            }
        }

        private void ClearViewModels()
        {
            foreach (var vm in _standardCards)
            {
                vm.Dispose();
            }
            foreach (var vm in _bonusCards)
            {
                vm.Dispose();
            }

            _standardCards.Clear();
            _bonusCards.Clear();
            _standardByRank.Clear();
            _bonusesByType.Clear();
            _handChanged.OnNext(Unit.Default);
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

        private static string NormalizeType(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType)
                ? "unknown-bonus"
                : bonusType.Trim();
        }
    }
}
