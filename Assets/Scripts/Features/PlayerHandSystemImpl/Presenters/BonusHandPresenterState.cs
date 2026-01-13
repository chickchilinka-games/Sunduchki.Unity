using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.BonusSystem.Config;
using Modules.BonusSystem.Data;
using Modules.BonusSystem.Services;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Services;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.Presenters
{
    public sealed class BonusHandPresenterState : IDisposable
    {
        private readonly BonusActionService _bonusService;
        private readonly DefenseDecisionService _defenseService;
        private readonly ITargetPlayerSelector _targetSelector;
        private readonly IBonusCardRulesProvider _rulesProvider;
        private readonly IBonusCardInfoProvider _infoProvider;
        private readonly ObservableList<BonusCardViewModel> _bonusCards = new();
        private readonly Dictionary<string, List<BonusCardViewModel>> _bonusesByType =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<BonusCardViewModel, IDisposable> _bonusSubscriptions = new();
        private readonly HashSet<string> _attackBonuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _defenseBonuses = new(StringComparer.OrdinalIgnoreCase);

        private IDisposable _defenseSubscription;
        private IDisposable _bonusExecutingSubscription;
        private bool _isLocalTurn;
        private string _localPlayerId = string.Empty;

        public IReadOnlyObservableList<BonusCardViewModel> BonusCards => _bonusCards;

        public BonusHandPresenterState(
            BonusActionService bonusService,
            DefenseDecisionService defenseService,
            ITargetPlayerSelector targetSelector,
            IBonusCardRulesProvider rulesProvider,
            IBonusCardInfoProvider infoProvider)
        {
            _bonusService = bonusService ?? throw new ArgumentNullException(nameof(bonusService));
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
            _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
            _infoProvider = infoProvider ?? throw new ArgumentNullException(nameof(infoProvider));
            FillRuleSets();
        }

        public bool Start(string localPlayerId)
        {
            if (string.IsNullOrWhiteSpace(localPlayerId))
            {
                Debug.unityLogger.LogWarning("PlayerHand", "Cannot start BonusHandPresenterState without a local player id.");
                return false;
            }

            _localPlayerId = localPlayerId;

            _defenseSubscription = _defenseService.Prompt
                .Subscribe(_ => UpdateBonusUsability());

            _bonusExecutingSubscription = _bonusService.IsExecuting
                .Subscribe(_ => UpdateBonusUsability());
            return true;
        }

        public void Stop()
        {
            _defenseSubscription?.Dispose();
            _defenseSubscription = null;
            _bonusExecutingSubscription?.Dispose();
            _bonusExecutingSubscription = null;

            foreach (var viewModel in _bonusCards)
            {
                UnsubscribeFromBonus(viewModel);
                viewModel.Dispose();
            }

            _bonusCards.Clear();
            _bonusesByType.Clear();
            _bonusSubscriptions.Clear();
            _localPlayerId = string.Empty;
            _isLocalTurn = false;
        }

        public void Dispose()
        {
            Stop();
        }

        public void SyncBonusCards(IReadOnlyList<BonusCardData> cards)
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
                    ApplyInfo(vm);
                    viewModels.Add(vm);
                    _bonusCards.Add(vm);
                    SubscribeToBonus(vm);
                }

                while (viewModels.Count > count)
                {
                    var idx = viewModels.Count - 1;
                    var vm = viewModels[idx];
                    viewModels.RemoveAt(idx);
                    _bonusCards.Remove(vm);
                    UnsubscribeFromBonus(vm);
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
                        UnsubscribeFromBonus(vm);
                        vm.Dispose();
                    }
                }
            }

            UpdateBonusUsability();
            UpdateAllInfo();
        }

        public void SetAttackTurn(bool isLocalTurn)
        {
            _isLocalTurn = isLocalTurn;
            UpdateBonusUsability();
        }

        private void SubscribeToBonus(BonusCardViewModel viewModel)
        {
            if (viewModel == null || _bonusSubscriptions.ContainsKey(viewModel))
            {
                return;
            }

            var subscription = viewModel.Use.Subscribe(_ => OnBonusPressed(viewModel));
            _bonusSubscriptions[viewModel] = subscription;
        }

        private void UnsubscribeFromBonus(BonusCardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            if (_bonusSubscriptions.Remove(viewModel, out var subscription))
            {
                subscription.Dispose();
            }
        }

        private void UpdateBonusUsability()
        {
            if (_bonusCards.Count == 0)
            {
                return;
            }

            var isExecuting = _bonusService.IsExecuting.CurrentValue;
            var prompt = _defenseService.CurrentPrompt;
            var defenseActive = prompt != null &&
                                !string.IsNullOrWhiteSpace(prompt.TargetId) &&
                                string.Equals(prompt.TargetId, _localPlayerId, StringComparison.Ordinal);

            HashSet<string> defenseOptions = null;
            if (defenseActive)
            {
                defenseOptions = new HashSet<string>(prompt.DefenseOptions ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);
            }

            foreach (var viewModel in _bonusCards)
            {
                var canUse = false;
                if (viewModel != null && !isExecuting)
                {
                    if (defenseActive)
                    {
                        canUse = defenseOptions != null &&
                                 _defenseBonuses.Contains(viewModel.BonusCardType) &&
                                 defenseOptions.Contains(viewModel.BonusCardType);
                    }
                    else if (_isLocalTurn)
                    {
                        canUse = _attackBonuses.Contains(viewModel.BonusCardType);
                    }
                }

                viewModel?.SetCanUse(canUse);
            }
        }

        private void ApplyInfo(BonusCardViewModel viewModel)
        {
            if (viewModel == null || _infoProvider == null)
            {
                return;
            }

            if (_infoProvider.TryGetInfo(viewModel.BonusCardType, out var info))
            {
                viewModel.SetInfo(info.Title, info.Description);
                return;
            }

            viewModel.SetInfo(string.Empty, string.Empty);
        }

        private void UpdateAllInfo()
        {
            foreach (var viewModel in _bonusCards)
            {
                ApplyInfo(viewModel);
            }
        }

        private void OnBonusPressed(BonusCardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            var bonusType = viewModel.BonusCardType;
            var prompt = _defenseService.CurrentPrompt;
            if (prompt != null &&
                string.Equals(prompt.TargetId, _localPlayerId, StringComparison.Ordinal))
            {
                _defenseService
                    .SubmitDecisionAsync(new DefenseDecisionSubmitRequest(true, bonusType))
                    .Forget();
                return;
            }

            if (!_isLocalTurn)
            {
                return;
            }

            UseAttackBonusAsync(bonusType).Forget();
        }

        private async UniTaskVoid UseAttackBonusAsync(string bonusType)
        {
            if (string.IsNullOrWhiteSpace(bonusType))
            {
                return;
            }

            var opponentId = await _targetSelector.SelectAsync();
            if (string.IsNullOrWhiteSpace(opponentId))
            {
                Debug.LogWarning("[PlayerHand] Cannot use bonus without opponent id.");
                return;
            }

            await _bonusService.UseBonusAsync(new BonusUseRequest(bonusType, opponentId));
        }

        private static string NormalizeType(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType)
                ? "unknown-bonus"
                : bonusType.Trim();
        }

        private void FillRuleSets()
        {
            _attackBonuses.Clear();
            _defenseBonuses.Clear();

            var attack = _rulesProvider.GetAttackBonusTypes() ?? Array.Empty<string>();
            foreach (var type in attack)
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    _attackBonuses.Add(type.Trim());
                }
            }

            var defense = _rulesProvider.GetDefenseBonusTypes() ?? Array.Empty<string>();
            foreach (var type in defense)
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    _defenseBonuses.Add(type.Trim());
                }
            }
        }
    }
}
