using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.Storage;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.BonusSystem.Data;
using Modules.BonusSystem.Services;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Services;
using R3;
using UnityEngine;
using Zenject;

namespace Features.PlayerHandSystemImpl.Rules
{
    public sealed class BonusHandUsePresentationRule : IInitializable, IDisposable
    {
        private readonly BonusCardViewModelStore _store;
        private readonly BonusActionService _bonusService;
        private readonly DefenseDecisionService _defenseService;
        private readonly ITargetPlayerSelector _targetSelector;
        private readonly PlayerHandPresentationContext _context;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly Dictionary<BonusCardViewModel, IDisposable> _useSubscriptions = new();

        public BonusHandUsePresentationRule(
            BonusCardViewModelStore store,
            BonusActionService bonusService,
            DefenseDecisionService defenseService,
            ITargetPlayerSelector targetSelector,
            PlayerHandPresentationContext context)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _bonusService = bonusService ?? throw new ArgumentNullException(nameof(bonusService));
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Initialize()
        {
            _context.IsActive
                .Subscribe(active =>
                {
                    if (!active)
                    {
                        ClearSubscriptions();
                        return;
                    }

                    RefreshSubscriptions();
                })
                .AddTo(_subscriptions);

            _store.Changed
                .Subscribe(_ => RefreshSubscriptions())
                .AddTo(_subscriptions);

            if (_context.IsActive.CurrentValue)
            {
                RefreshSubscriptions();
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            ClearSubscriptions();
        }

        private void RefreshSubscriptions()
        {
            var active = new HashSet<BonusCardViewModel>(_store.Items);

            var stale = new List<BonusCardViewModel>();
            foreach (var entry in _useSubscriptions)
            {
                if (!active.Contains(entry.Key))
                {
                    stale.Add(entry.Key);
                }
            }

            foreach (var vm in stale)
            {
                if (_useSubscriptions.Remove(vm, out var sub))
                {
                    sub.Dispose();
                }
            }

            foreach (var vm in active)
            {
                if (vm == null || _useSubscriptions.ContainsKey(vm))
                {
                    continue;
                }

                _useSubscriptions[vm] = vm.Use.Subscribe(_ => OnBonusPressed(vm));
            }
        }

        private void ClearSubscriptions()
        {
            foreach (var entry in _useSubscriptions)
            {
                entry.Value.Dispose();
            }

            _useSubscriptions.Clear();
        }

        private void OnBonusPressed(BonusCardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            var bonusType = viewModel.BonusCardType;
            var prompt = _defenseService.CurrentPrompt;
            var localId = _context.LocalPlayerId.CurrentValue;

            if (prompt != null &&
                !string.IsNullOrWhiteSpace(localId) &&
                string.Equals(prompt.TargetId, localId, StringComparison.Ordinal))
            {
                _defenseService
                    .SubmitDecisionAsync(new DefenseDecisionSubmitRequest(true, bonusType))
                    .Forget();
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
    }
}
