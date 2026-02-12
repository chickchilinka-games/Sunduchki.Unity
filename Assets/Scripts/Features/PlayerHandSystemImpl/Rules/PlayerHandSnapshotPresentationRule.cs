using System;
using Features.PlayerHandSystemImpl.Presenters;
using Modules.Lobby.Services;
using Modules.PlayerHand.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Rules
{
    public sealed class PlayerHandSnapshotPresentationRule : IInitializable, IDisposable
    {
        private readonly CompositeDisposable _subscriptions = new();
        private readonly PlayerHandService _handService;
        private readonly LobbyService _lobbyService;
        private readonly BonusHandPresenter _bonusPresenter;
        private readonly PlayerHandPresenter _presenter;
        private IDisposable _handSubscription;
        private string _localPlayerId = string.Empty;

        public PlayerHandSnapshotPresentationRule(
            PlayerHandPresenter presenter,
            BonusHandPresenter bonusPresenter,
            PlayerHandService handService,
            LobbyService lobbyService)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _bonusPresenter = bonusPresenter ?? throw new ArgumentNullException(nameof(bonusPresenter));
            _handService = handService ?? throw new ArgumentNullException(nameof(handService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        public void Initialize()
        {
            _lobbyService.Players
                .Subscribe(_ => EnsureHandSubscription())
                .AddTo(_subscriptions);

            _presenter.IsActive
                .Subscribe(_ => EnsureHandSubscription())
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _handSubscription?.Dispose();
            _handSubscription = null;
        }

        private void EnsureHandSubscription()
        {
            if (!_presenter.IsActive.CurrentValue)
            {
                _handSubscription?.Dispose();
                _handSubscription = null;
                return;
            }

            var localPlayer = _lobbyService.GetLocalPlayer();
            var localId = localPlayer.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(localId))
            {
                return;
            }

            if (string.Equals(_localPlayerId, localId, StringComparison.Ordinal) && _handSubscription != null)
            {
                return;
            }

            _handSubscription?.Dispose();
            _localPlayerId = localId;
            _presenter.SetLocalPlayerId(localId);
            _handSubscription = _handService
                .ObserveHand(localId)
                .Subscribe(state =>
                {
                    _presenter.OnHandUpdated(state);
                    _bonusPresenter.SyncBonusCards(state.BonusCards);
                });
        }
    }
}
