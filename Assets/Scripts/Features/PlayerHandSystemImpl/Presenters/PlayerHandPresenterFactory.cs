using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.BonusSystem.Config;
using Modules.BonusSystem.Services;
using Modules.CardRequestSystem.Services;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Services;
using Modules.TurnSystem.Services;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.Presenters
{
    /// <summary>
    /// Stateless factory that produces presenter state for the player hand view.
    /// </summary>
    public class PlayerHandPresenterFactory
    {
        private readonly PlayerHandService _playerHandService;
        private readonly LobbyService _lobbyService;
        private readonly TurnSequenceService _turnService;
        private readonly CardRequestCommandService _cardRequestService;
        private readonly BonusActionService _bonusService;
        private readonly DefenseDecisionService _defenseService;
        private readonly ITargetPlayerSelector _targetSelector;
        private readonly IBonusCardRulesProvider _rulesProvider;
        private readonly IBonusCardInfoProvider _infoProvider;

        public PlayerHandPresenterFactory(
            PlayerHandService playerHandService,
            LobbyService lobbyService,
            TurnSequenceService turnService,
            CardRequestCommandService cardRequestService,
            BonusActionService bonusService,
            DefenseDecisionService defenseService,
            ITargetPlayerSelector targetSelector,
            IBonusCardRulesProvider rulesProvider,
            IBonusCardInfoProvider infoProvider)
        {
            _playerHandService = playerHandService ?? throw new ArgumentNullException(nameof(playerHandService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _bonusService = bonusService ?? throw new ArgumentNullException(nameof(bonusService));
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
            _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
            _infoProvider = infoProvider ?? throw new ArgumentNullException(nameof(infoProvider));
        }

        public PlayerHandPresenterState CreateState()
        {
            return new PlayerHandPresenterState(
                _playerHandService,
                _lobbyService,
                _turnService,
                _cardRequestService,
                _bonusService,
                _defenseService,
                _targetSelector,
                _rulesProvider,
                _infoProvider);
        }
    }

    /// <summary>
    /// Presenter state that keeps track of view-models for the hand.
    /// </summary>
    public sealed class PlayerHandPresenterState : IDisposable
    {
        private readonly PlayerHandService _playerHandService;
        private readonly LobbyService _lobbyService;
        private readonly TurnSequenceService _turnService;
        private readonly CardRequestCommandService _cardRequestService;
        private readonly DefenseDecisionService _defenseService;
        private readonly ObservableList<StandardCardViewModel> _standardCards = new();
        private readonly Dictionary<string, StandardCardViewModel> _standardByRank = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<StandardCardViewModel, IDisposable> _pressSubscriptions = new();
        private readonly Subject<Unit> _handChanged = new();
        private readonly BonusHandPresenterState _bonusPresenter;

        private IDisposable _handSubscription;
        private IDisposable _turnSubscription;
        private IDisposable _defenseSubscription;
        private IDisposable _chestSubscription;
        private bool _isLocalTurn;
        private bool _isDefenseActive;
        private string _defenseRank = string.Empty;

        public IReadOnlyObservableList<StandardCardViewModel> StandardCards => _standardCards;
        public IReadOnlyObservableList<BonusCardViewModel> BonusCards => _bonusPresenter.BonusCards;
        public Observable<Unit> HandChanged => _handChanged;

        public PlayerHandPresenterState(
            PlayerHandService playerHandService,
            LobbyService lobbyService,
            TurnSequenceService turnService,
            CardRequestCommandService cardRequestService,
            BonusActionService bonusService,
            DefenseDecisionService defenseService,
            ITargetPlayerSelector targetSelector,
            IBonusCardRulesProvider rulesProvider,
            IBonusCardInfoProvider infoProvider)
        {
            _playerHandService = playerHandService ?? throw new ArgumentNullException(nameof(playerHandService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _bonusPresenter = new BonusHandPresenterState(
                bonusService,
                defenseService,
                targetSelector,
                rulesProvider,
                infoProvider);
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
                Debug.unityLogger.LogWarning("PlayerHand", "Cannot start PlayerHandPresenterState without a local player id.");
                return false;
            }

            _handSubscription = _playerHandService
                .ObserveHand(localPlayerId)
                .Subscribe(UpdateHand);

            _turnSubscription = _turnService.State
                .Subscribe(state => ApplyPressable(state.IsLocalTurn));

            _defenseSubscription = _defenseService.Prompt
                .Subscribe(prompt => UpdateDefenseState(prompt, localPlayerId));
            UpdateDefenseState(_defenseService.CurrentPrompt, localPlayerId);

            _chestSubscription = _lobbyService.ChestUpdated
                .Subscribe(payload => OnSetCompleted(payload, localPlayerId));

            if (!_bonusPresenter.Start(localPlayerId))
            {
                return false;
            }
            return true;
        }

        public void Stop()
        {
            _handSubscription?.Dispose();
            _handSubscription = null;
            _turnSubscription?.Dispose();
            _turnSubscription = null;
            _defenseSubscription?.Dispose();
            _defenseSubscription = null;
            _chestSubscription?.Dispose();
            _chestSubscription = null;
            _bonusPresenter.Stop();
            ClearViewModels();
        }

        public void Dispose()
        {
            Stop();
        }

        private void UpdateHand(PlayerHandState state)
        {
            SyncStandardCards(state.StandardCards);
            _bonusPresenter.SyncBonusCards(state.BonusCards);
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
                    SubscribeToPress(viewModel);
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
                    UnsubscribeFromPress(viewModel);
                    viewModel.Dispose();
                }
            }

            ApplyPressable(_isLocalTurn);
        }

        private void ClearViewModels()
        {
            foreach (var vm in _standardCards)
            {
                UnsubscribeFromPress(vm);
                vm.Dispose();
            }

            _standardCards.Clear();
            _standardByRank.Clear();
            _handChanged.OnNext(Unit.Default);
        }

        private void ApplyPressable(bool isLocalTurn)
        {
            _isLocalTurn = isLocalTurn;
            UpdateStandardUsability();

            _bonusPresenter.SetAttackTurn(isLocalTurn);
        }

        private void SubscribeToPress(StandardCardViewModel viewModel)
        {
            if (viewModel == null || _pressSubscriptions.ContainsKey(viewModel))
            {
                return;
            }

            var subscription = viewModel.Pressed.Subscribe(_ => OnCardPressed(viewModel));
            _pressSubscriptions[viewModel] = subscription;
        }

        private void UnsubscribeFromPress(StandardCardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            if (_pressSubscriptions.Remove(viewModel, out var subscription))
            {
                subscription.Dispose();
            }
        }

        private void OnCardPressed(StandardCardViewModel viewModel)
        {
            if (viewModel == null)
            {
                return;
            }

            if (_isDefenseActive)
            {
                if (!string.Equals(viewModel.Rank, _defenseRank, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _defenseService
                    .SubmitDecisionAsync(new DefenseDecisionSubmitRequest(false, null))
                    .Forget();
                return;
            }

            if (!_isLocalTurn)
            {
                return;
            }

            var opponentId = ResolveOpponentId();
            if (string.IsNullOrWhiteSpace(opponentId))
            {
                Debug.LogWarning("[PlayerHand] Cannot send card request without opponent id.");
                return;
            }

            var rank = ToServerRank(viewModel.Rank);
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            _cardRequestService.AskAsync(rank, opponentId).Forget();
        }

        private void UpdateDefenseState(DefenseDecisionPrompt prompt, string localPlayerId)
        {
            if (prompt != null &&
                !string.IsNullOrWhiteSpace(prompt.TargetId) &&
                string.Equals(prompt.TargetId, localPlayerId, StringComparison.Ordinal))
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
            foreach (var viewModel in _standardCards)
            {
                var canPress = _isDefenseActive
                    ? string.Equals(viewModel.Rank, _defenseRank, StringComparison.OrdinalIgnoreCase)
                    : _isLocalTurn;
                viewModel.SetPressable(canPress);
            }
        }

        private void OnSetCompleted(LobbyChestUpdatedPayload payload, string localPlayerId)
        {
            if (payload.PlayerId == null ||
                !string.Equals(payload.PlayerId, localPlayerId, StringComparison.Ordinal))
            {
                return;
            }

            var rankKey = NormalizeRank(payload.Rank);
            if (_standardByRank.TryGetValue(rankKey, out var viewModel))
            {
                viewModel.MarkSetCompleted();
            }
        }

        private string ResolveOpponentId()
        {
            var players = _lobbyService.Players?.CurrentValue;
            if (players == null)
            {
                return ResolveOpponentFromTurnState(string.Empty);
            }

            var localId = _lobbyService.StateContext.Data.PlayerId ?? string.Empty;
            foreach (var player in players)
            {
                if (string.IsNullOrWhiteSpace(player.Id))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(localId) &&
                    string.Equals(player.Id, localId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!player.IsLocal || string.IsNullOrWhiteSpace(localId))
                {
                    return player.Id;
                }
            }

            return ResolveOpponentFromTurnState(localId);
        }

        private string ResolveOpponentFromTurnState(string localId)
        {
            var turnState = _turnService.State.CurrentValue;
            var previous = turnState.PreviousPlayerId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(previous) &&
                !string.Equals(previous, localId, StringComparison.Ordinal))
            {
                return previous;
            }

            return string.Empty;
        }

        private static string ToServerRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return string.Empty;
            }

            return rank.Trim().ToLowerInvariant() switch
            {
                "two" => "Two",
                "three" => "Three",
                "four" => "Four",
                "five" => "Five",
                "six" => "Six",
                "seven" => "Seven",
                "eight" => "Eight",
                "nine" => "Nine",
                "ten" => "Ten",
                "jack" => "Jack",
                "queen" => "Queen",
                "king" => "King",
                "ace" => "Ace",
                _ => string.Empty
            };
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

    }
}
