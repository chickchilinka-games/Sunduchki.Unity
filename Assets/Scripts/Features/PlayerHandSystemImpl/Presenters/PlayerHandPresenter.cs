using System;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presenters
{
    /// <summary>
    /// Coordinates the lifetime of player-hand presenter state based on lobby game events.
    /// </summary>
    public sealed class PlayerHandPresenter : IInitializable, IDisposable
    {
        private readonly PlayerHandPresenterFactory _factory;
        private readonly LobbyService _lobbyService;
        private readonly ReactiveProperty<PlayerHandPresenterState> _stateProperty = new(null);
        private readonly CompositeDisposable _subscriptions = new();

        private PlayerHandPresenterState _activeState;

        public PlayerHandPresenter(PlayerHandPresenterFactory factory, LobbyService lobbyService)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        public ReadOnlyReactiveProperty<PlayerHandPresenterState> State => _stateProperty;
        public PlayerHandPresenterState CurrentState => _stateProperty.Value;

        public void Initialize()
        {
            _lobbyService.GameStarted
                .Subscribe(_ => StartState())
                .AddTo(_subscriptions);

            _lobbyService.GameEnded
                .Subscribe(_ => StopState())
                .AddTo(_subscriptions);

            _lobbyService.State
                .Subscribe(state =>
                {
                    if (state.Status != LobbyStatus.Started && !state.Started)
                    {
                        StopState();
                    }
                })
                .AddTo(_subscriptions);

            var state = _lobbyService.State.CurrentValue;
            if (state.Status == LobbyStatus.Started || state.Started)
            {
                StartState();
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            StopState();
            _stateProperty.Dispose();
        }

        private void StartState()
        {
            if (_activeState != null)
            {
                return;
            }

            var state = _factory.CreateState();
            if (!state.Start())
            {
                state.Dispose();
                return;
            }

            _activeState = state;
            _stateProperty.Value = state;
        }

        private void StopState()
        {
            if (_activeState == null)
            {
                return;
            }

            var state = _activeState;
            _activeState = null;
            _stateProperty.Value = null;

            state.Stop();
            state.Dispose();
        }
    }
}
