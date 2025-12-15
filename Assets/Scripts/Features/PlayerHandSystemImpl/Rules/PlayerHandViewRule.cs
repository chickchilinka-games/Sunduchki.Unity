using System;
using Features.PlayerHandSystemImpl.Service;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Rules
{
    /// <summary>
    /// Coordinates the lifetime of player-hand view runtimes based on lobby game events.
    /// </summary>
    public sealed class PlayerHandViewRule : IInitializable, IDisposable
    {
        private readonly PlayerHandViewService _viewService;
        private readonly LobbyService _lobbyService;
        private readonly ReactiveProperty<PlayerHandViewRuntime> _runtimeProperty = new(null);
        private readonly CompositeDisposable _subscriptions = new();

        private PlayerHandViewRuntime _activeRuntime;

        public PlayerHandViewRule(PlayerHandViewService viewService, LobbyService lobbyService)
        {
            _viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        public ReadOnlyReactiveProperty<PlayerHandViewRuntime> Runtime => _runtimeProperty;
        public PlayerHandViewRuntime CurrentRuntime => _runtimeProperty.Value;

        public void Initialize()
        {
            _lobbyService.GameStarted
                .Subscribe(_ => StartRuntime())
                .AddTo(_subscriptions);

            _lobbyService.GameEnded
                .Subscribe(_ => StopRuntime())
                .AddTo(_subscriptions);

            _lobbyService.State
                .Subscribe(state =>
                {
                    if (state.Status != LobbyStatus.Started && !state.Started)
                    {
                        StopRuntime();
                    }
                })
                .AddTo(_subscriptions);

            var state = _lobbyService.State.CurrentValue;
            if (state.Status == LobbyStatus.Started || state.Started)
            {
                StartRuntime();
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            StopRuntime();
            _runtimeProperty.Dispose();
        }

        private void StartRuntime()
        {
            if (_activeRuntime != null)
            {
                return;
            }

            var runtime = _viewService.CreateRuntime();
            if (!runtime.Start())
            {
                runtime.Dispose();
                return;
            }

            _activeRuntime = runtime;
            _runtimeProperty.Value = runtime;
        }

        private void StopRuntime()
        {
            if (_activeRuntime == null)
            {
                return;
            }

            var runtime = _activeRuntime;
            _activeRuntime = null;
            _runtimeProperty.Value = null;

            runtime.Stop();
            runtime.Dispose();
        }
    }
}
