using System;
using System.Collections.Generic;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Modules.DefenseDecisionSystem.Data;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Presenters
{
    public sealed class BonusHandInteractionPresenter : IInitializable, IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly BonusCardViewModelStorage _storage;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly HashSet<string> _attackBonuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _defenseBonuses = new(StringComparer.OrdinalIgnoreCase);

        private bool _isLocalTurn;
        private string _localPlayerId = string.Empty;
        private DefenseDecisionPrompt _currentPrompt;
        private bool _isBonusExecuting;

        public BonusHandInteractionPresenter(
            PlayerHandPresentationContext context,
            BonusCardViewModelStorage storage)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Initialize()
        {
            _context.IsActive
                .Subscribe(_ => UpdateBonusUsability())
                .AddTo(_subscriptions);

            _context.LocalPlayerId
                .Subscribe(localId =>
                {
                    if (string.IsNullOrWhiteSpace(localId))
                    {
                        return;
                    }

                    _localPlayerId = localId;
                    UpdateBonusUsability();
                })
                .AddTo(_subscriptions);

            _storage.Changed
                .Subscribe(_ => UpdateBonusUsability())
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        public void SetAttackTurn(bool isLocalTurn)
        {
            _isLocalTurn = isLocalTurn;
            UpdateBonusUsability();
        }

        public void SetDefensePrompt(DefenseDecisionPrompt prompt)
        {
            _currentPrompt = prompt;
            UpdateBonusUsability();
        }

        public void SetBonusExecuting(bool isExecuting)
        {
            _isBonusExecuting = isExecuting;
            UpdateBonusUsability();
        }

        public void SetBonusRules(IEnumerable<string> attackTypes, IEnumerable<string> defenseTypes)
        {
            _attackBonuses.Clear();
            _defenseBonuses.Clear();

            if (attackTypes != null)
            {
                foreach (var type in attackTypes)
                {
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        _attackBonuses.Add(type.Trim());
                    }
                }
            }

            if (defenseTypes != null)
            {
                foreach (var type in defenseTypes)
                {
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        _defenseBonuses.Add(type.Trim());
                    }
                }
            }

            UpdateBonusUsability();
        }

        private void UpdateBonusUsability()
        {
            if (!_context.IsActive.CurrentValue)
            {
                foreach (var viewModel in _storage.Items)
                {
                    viewModel?.SetCanUse(false);
                }
                return;
            }

            if (_storage.Items.Count == 0)
            {
                return;
            }

            var isExecuting = _isBonusExecuting;
            var prompt = _currentPrompt;
            var defenseActive = prompt != null &&
                                !string.IsNullOrWhiteSpace(prompt.TargetId) &&
                                string.Equals(prompt.TargetId, _localPlayerId, StringComparison.Ordinal);

            HashSet<string> defenseOptions = null;
            if (defenseActive)
            {
                defenseOptions = new HashSet<string>(prompt.DefenseOptions ?? Array.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);
            }

            foreach (var viewModel in _storage.Items)
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
    }
}
