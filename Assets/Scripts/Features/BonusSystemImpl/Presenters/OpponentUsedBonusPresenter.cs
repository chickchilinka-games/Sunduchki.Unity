using System;
using System.Collections.Generic;
using Modules.BonusSystem.Config;
using Modules.BonusSystem.Data;
using Modules.BonusSystem.Interfaces;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Services;
using Modules.Lobby.Services;
using Modules.TurnSystem.Services;
using R3;
using Zenject;
using Features.BonusSystemImpl.ViewModel;

namespace Features.BonusSystemImpl.Presenters
{
    public sealed class OpponentUsedBonusPresenter : IInitializable, IDisposable
    {
        private static readonly string[] EndOfTurnAttackBonuses =
        {
            "AskTwice",
            "StealExtraOnSuccess",
            "SilentAsk"
        };

        private const float InstantHideDelay = 1.2f;
        private const float DefenseHideDelay = 1.2f;

        private readonly IBonusUsageEventSource _bonusUsageEventSource;
        private readonly LobbyService _lobbyService;
        private readonly TurnSequenceService _turnService;
        private readonly CardRequestService _cardRequestService;
        private readonly IBonusCardRulesProvider _rulesProvider;

        private readonly HashSet<string> _defenseBonuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _endOfTurnBonuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly CompositeDisposable _subscriptions = new();
        private readonly ReactiveProperty<OpponentUsedBonusViewState> _state =
            new(OpponentUsedBonusViewState.Hidden);

        private bool _holdUntilTurnEnd;
        private string _holderPlayerId = string.Empty;
        private int _stateRevision;

        public OpponentUsedBonusPresenter(
            IBonusUsageEventSource bonusUsageEventSource,
            LobbyService lobbyService,
            TurnSequenceService turnService,
            CardRequestService cardRequestService,
            IBonusCardRulesProvider rulesProvider)
        {
            _bonusUsageEventSource = bonusUsageEventSource ?? throw new ArgumentNullException(nameof(bonusUsageEventSource));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
        }

        public ReadOnlyReactiveProperty<OpponentUsedBonusViewState> State => _state;

        public void Initialize()
        {
            CacheRules();

            _bonusUsageEventSource.BonusUsed
                .Subscribe(OnBonusUsed)
                .AddTo(_subscriptions);

            _turnService.State
                .Subscribe(state => OnTurnChanged(state.CurrentPlayerId))
                .AddTo(_subscriptions);

            _cardRequestService.State
                .Subscribe(OnCardRequestState)
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void CacheRules()
        {
            _defenseBonuses.Clear();
            var defense = _rulesProvider.GetDefenseBonusTypes() ?? Array.Empty<string>();
            foreach (var type in defense)
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    _defenseBonuses.Add(type.Trim());
                }
            }

            _endOfTurnBonuses.Clear();
            foreach (var type in EndOfTurnAttackBonuses)
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    _endOfTurnBonuses.Add(type.Trim());
                }
            }
        }

        private void OnBonusUsed(BonusUsedEvent payload)
        {
            if (string.IsNullOrWhiteSpace(payload.BonusType))
            {
                return;
            }

            var localId = _lobbyService.GetLocalPlayerId();
            if (string.IsNullOrWhiteSpace(localId))
            {
                return;
            }

            if (string.Equals(payload.PlayerId, localId, StringComparison.Ordinal))
            {
                return;
            }

            var normalized = payload.BonusType.Trim();
            if (_defenseBonuses.Contains(normalized))
            {
                ShowTimed(normalized, DefenseHideDelay, payload.PlayerId);
                return;
            }

            if (_endOfTurnBonuses.Contains(normalized))
            {
                ShowHold(normalized, payload.PlayerId);
                return;
            }

            ShowTimed(normalized, InstantHideDelay, payload.PlayerId);
        }

        private void OnTurnChanged(string currentPlayerId)
        {
            if (!_holdUntilTurnEnd)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_holderPlayerId))
            {
                Hide();
                return;
            }

            if (!string.Equals(_holderPlayerId, currentPlayerId, StringComparison.Ordinal))
            {
                Hide();
            }
        }

        private void OnCardRequestState(CardRequestState state)
        {
            if (!state.HasEvent)
            {
                return;
            }

            var evt = state.LastEvent;
            if (evt.EventType != CardRequestEventType.Transferred &&
                evt.EventType != CardRequestEventType.Denied)
            {
                return;
            }

            if (_holdUntilTurnEnd &&
                !string.IsNullOrWhiteSpace(evt.AskerId) &&
                string.Equals(_holderPlayerId, evt.AskerId, StringComparison.Ordinal))
            {
                Hide();
            }
        }

        private void ShowTimed(string bonusType, float hideDelay, string playerId)
        {
            _holdUntilTurnEnd = false;
            _holderPlayerId = string.Empty;
            _state.Value = new OpponentUsedBonusViewState(true, bonusType, hideDelay, NextRevision());
        }

        private void ShowHold(string bonusType, string playerId)
        {
            _holdUntilTurnEnd = true;
            _holderPlayerId = playerId ?? string.Empty;
            _state.Value = new OpponentUsedBonusViewState(true, bonusType, null, NextRevision());
        }

        private void Hide()
        {
            _holdUntilTurnEnd = false;
            _holderPlayerId = string.Empty;
            _state.Value = new OpponentUsedBonusViewState(false, string.Empty, null, NextRevision());
        }

        private int NextRevision()
        {
            if (_stateRevision == int.MaxValue)
            {
                _stateRevision = 0;
            }
            else
            {
                _stateRevision++;
            }

            return _stateRevision;
        }
    }
}
