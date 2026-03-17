using System;
using Features.PlayerHandSystemImpl.Presentation.Presenters;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Modules.BonusSystem.Services;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using Modules.CardRequestSystem.Services;
using Modules.PlayerHand.Services;
using Modules.TurnSystem.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Rules
{
    public sealed class PlayerHandPresentationCoordinatorRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly TurnSequenceService _turnService;
        private readonly DefenseDecisionService _defenseService;
        private readonly CardRequestService _cardRequestService;
        private readonly PlayerHandService _handService;
        private readonly BonusActionService _bonusService;
        private readonly PlayerHandPresentationContext _context;
        private readonly PlayerHandPresenter _playerHandPresenter;
        private readonly BonusHandPresenter _bonusHandPresenter;
        private readonly PlayerHandInteractionPresenter _playerHandInteractionPresenter;
        private readonly BonusHandInteractionPresenter _bonusHandInteractionPresenter;
        private readonly CardRequestPresentationPresenter _cardRequestPresenter;
        private readonly CompositeDisposable _subscriptions = new();

        private IDisposable _handSubscription;
        private string _subscribedPlayerId = string.Empty;

        public PlayerHandPresentationCoordinatorRule(
            LobbyService lobbyService,
            TurnSequenceService turnService,
            DefenseDecisionService defenseService,
            CardRequestService cardRequestService,
            PlayerHandService handService,
            BonusActionService bonusService,
            PlayerHandPresentationContext context,
            PlayerHandPresenter playerHandPresenter,
            BonusHandPresenter bonusHandPresenter,
            PlayerHandInteractionPresenter playerHandInteractionPresenter,
            BonusHandInteractionPresenter bonusHandInteractionPresenter,
            CardRequestPresentationPresenter cardRequestPresenter)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _handService = handService ?? throw new ArgumentNullException(nameof(handService));
            _bonusService = bonusService ?? throw new ArgumentNullException(nameof(bonusService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _playerHandPresenter = playerHandPresenter ?? throw new ArgumentNullException(nameof(playerHandPresenter));
            _bonusHandPresenter = bonusHandPresenter ?? throw new ArgumentNullException(nameof(bonusHandPresenter));
            _playerHandInteractionPresenter = playerHandInteractionPresenter ?? throw new ArgumentNullException(nameof(playerHandInteractionPresenter));
            _bonusHandInteractionPresenter = bonusHandInteractionPresenter ?? throw new ArgumentNullException(nameof(bonusHandInteractionPresenter));
            _cardRequestPresenter = cardRequestPresenter ?? throw new ArgumentNullException(nameof(cardRequestPresenter));
        }

        public void Initialize()
        {
            _lobbyService.GameStarted
                .Subscribe(_ => ActivateFromLobby())
                .AddTo(_subscriptions);

            _lobbyService.State
                .Subscribe(OnLobbyStateChanged)
                .AddTo(_subscriptions);

            _lobbyService.Players
                .Subscribe(_ => EnsureHandSubscription())
                .AddTo(_subscriptions);

            _context.IsActive
                .Subscribe(active =>
                {
                    _cardRequestPresenter.ResetSession();
                    EnsureHandSubscription();
                    if (active)
                    {
                        _cardRequestPresenter.OnCardRequestStateChanged(_cardRequestService.Current);
                    }
                })
                .AddTo(_subscriptions);

            _turnService.State
                .Subscribe(state =>
                {
                    _playerHandInteractionPresenter.OnTurnStateChanged(state.IsLocalTurn);
                    _bonusHandInteractionPresenter.SetAttackTurn(state.IsLocalTurn);
                })
                .AddTo(_subscriptions);

            _defenseService.Prompt
                .Subscribe(prompt =>
                {
                    _playerHandInteractionPresenter.OnDefensePromptChanged(prompt);
                    _bonusHandInteractionPresenter.SetDefensePrompt(prompt);
                })
                .AddTo(_subscriptions);

            _cardRequestService.State
                .Subscribe(state => _cardRequestPresenter.OnCardRequestStateChanged(state))
                .AddTo(_subscriptions);

            _handService.CardsReceived
                .Subscribe(evt => _cardRequestPresenter.OnCardsReceivedEvent(evt))
                .AddTo(_subscriptions);

            _bonusService.IsExecuting
                .Subscribe(isExecuting => _bonusHandInteractionPresenter.SetBonusExecuting(isExecuting))
                .AddTo(_subscriptions);

            _playerHandInteractionPresenter.OnTurnStateChanged(_turnService.State.CurrentValue.IsLocalTurn);
            _bonusHandInteractionPresenter.SetAttackTurn(_turnService.State.CurrentValue.IsLocalTurn);
            _playerHandInteractionPresenter.OnDefensePromptChanged(_defenseService.CurrentPrompt);
            _bonusHandInteractionPresenter.SetDefensePrompt(_defenseService.CurrentPrompt);
            _bonusHandInteractionPresenter.SetBonusExecuting(_bonusService.IsExecuting.CurrentValue);

            var lobbyState = _lobbyService.State.CurrentValue;
            if (lobbyState.Status == LobbyStatus.Started || lobbyState.Started)
            {
                ActivateFromLobby();
            }

            EnsureHandSubscription();
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _handSubscription?.Dispose();
            _handSubscription = null;
            _subscribedPlayerId = string.Empty;
        }

        private void OnLobbyStateChanged(LobbyState state)
        {
            if (state.Status == LobbyStatus.Ended)
            {
                return;
            }

            if (state.Status == LobbyStatus.Started || state.Started)
            {
                ActivateFromLobby();
                return;
            }

            if (state.Status != LobbyStatus.Started && !state.Started)
            {
                _context.Clear();
                EnsureHandSubscription();
            }
        }

        private void ActivateFromLobby()
        {
            var localId = _lobbyService.GetLocalPlayer().Id ?? string.Empty;
            _context.SetLocalPlayerId(localId);
            _context.SetAwaitingAsk(false);
            _context.SetActive(true);
            EnsureHandSubscription();
        }

        private void EnsureHandSubscription()
        {
            if (!_context.IsActive.CurrentValue)
            {
                _handSubscription?.Dispose();
                _handSubscription = null;
                _subscribedPlayerId = string.Empty;
                return;
            }

            var localId = _lobbyService.GetLocalPlayer().Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(localId))
            {
                return;
            }

            if (string.Equals(_subscribedPlayerId, localId, StringComparison.Ordinal) && _handSubscription != null)
            {
                return;
            }

            _handSubscription?.Dispose();
            _subscribedPlayerId = localId;
            _playerHandPresenter.SetLocalPlayerId(localId);
            _handSubscription = _handService.ObserveHand(localId).Subscribe(state =>
            {
                _playerHandPresenter.OnHandUpdated(state);
                _bonusHandPresenter.SyncBonusCards(state.BonusCards);
            });
        }
    }
}
