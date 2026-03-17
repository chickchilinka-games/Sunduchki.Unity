using System;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Modules.DefenseDecisionSystem.Data;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Presenters
{
    public sealed class PlayerHandInteractionPresenter : IInitializable, IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly RankStackViewModelStorage _storage;
        private readonly CompositeDisposable _subscriptions = new();

        private bool _isLocalTurn;
        private bool _isDefenseActive;
        private string _defenseRank = string.Empty;
        private string _localPlayerId = string.Empty;
        private DefenseDecisionPrompt _currentPrompt;

        public PlayerHandInteractionPresenter(
            PlayerHandPresentationContext context,
            RankStackViewModelStorage storage)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Initialize()
        {
            _context.IsActive
                .Subscribe(active =>
                {
                    if (!active)
                    {
                        ResetSessionState();
                    }

                    UpdateStandardUsability();
                })
                .AddTo(_subscriptions);

            _context.LocalPlayerId
                .Subscribe(localId =>
                {
                    if (string.IsNullOrWhiteSpace(localId))
                    {
                        return;
                    }

                    _localPlayerId = localId;
                    if (_currentPrompt != null)
                    {
                        UpdateDefenseState(_currentPrompt);
                    }
                })
                .AddTo(_subscriptions);

            _context.IsAwaitingAsk
                .Subscribe(_ => UpdateStandardUsability())
                .AddTo(_subscriptions);

            _storage.Changed
                .Subscribe(_ => UpdateStandardUsability())
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        public void OnTurnStateChanged(bool isLocalTurn)
        {
            if (!_context.IsActive.CurrentValue)
            {
                return;
            }

            _isLocalTurn = isLocalTurn;
            UpdateStandardUsability();
        }

        public void OnDefensePromptChanged(DefenseDecisionPrompt prompt)
        {
            if (!_context.IsActive.CurrentValue)
            {
                return;
            }

            _currentPrompt = prompt;
            UpdateDefenseState(prompt);
        }

        private void ResetSessionState()
        {
            _currentPrompt = null;
            _isLocalTurn = false;
            _isDefenseActive = false;
            _defenseRank = string.Empty;
        }

        private void UpdateDefenseState(DefenseDecisionPrompt prompt)
        {
            if (prompt != null &&
                !string.IsNullOrWhiteSpace(prompt.TargetId) &&
                string.Equals(prompt.TargetId, _localPlayerId, StringComparison.Ordinal))
            {
                _isDefenseActive = true;
                _defenseRank = NormalizeRank(prompt.Rank);
            }
            else
            {
                _isDefenseActive = false;
                _defenseRank = string.Empty;
            }

            UpdateStandardUsability();
        }

        private void UpdateStandardUsability()
        {
            if (!_context.IsActive.CurrentValue)
            {
                foreach (var viewModel in _storage.Items)
                {
                    viewModel.SetPressable(false);
                }
                return;
            }

            var awaitingAsk = _context.IsAwaitingAsk.CurrentValue;
            foreach (var viewModel in _storage.Items)
            {
                var canPress = _isDefenseActive
                    ? string.Equals(viewModel.Rank, _defenseRank, StringComparison.OrdinalIgnoreCase)
                    : _isLocalTurn;
                if (awaitingAsk)
                {
                    canPress = false;
                }

                viewModel.SetPressable(canPress);
            }
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank)
                ? "unknown"
                : rank.Trim().ToLowerInvariant();
        }
    }
}
