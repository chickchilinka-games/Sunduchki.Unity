using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Model;
using Modules.SignalR.Config;
using R3;
using Zenject;

namespace Modules.CardRequestSystem.Rules
{
    public class TrackCardRequestsOnLobbyConnectRule : IInitializable, IDisposable
    {
        private readonly LobbyStateContext _lobbyState;
        private readonly ICardRequestSignalClient _signalClient;
        private readonly ICardRequestSignalHandler _signalHandler;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _cts;
        private IDisposable _subscription;

        public TrackCardRequestsOnLobbyConnectRule(
            LobbyStateContext lobbyState,
            ICardRequestSignalClient signalClient,
            ICardRequestSignalHandler signalHandler,
            IGameHubConfigProvider configProvider)
        {
            _lobbyState = lobbyState;
            _signalClient = signalClient;
            _signalHandler = signalHandler;
            _configProvider = configProvider;
        }

        public void Initialize()
        {
            _lobbyState.State
                .Subscribe(OnLobbyStateChanged)
                .AddTo(_disposables);
        }

        private void OnLobbyStateChanged(LobbyState state)
        {
            switch (state.Status)
            {
                case LobbyStatus.Started:
                    BeginTracking().Forget();
                    break;
                default:
                    StopTracking().Forget();
                    break;
            }
        }

        private async UniTaskVoid BeginTracking()
        {
            var config = _lobbyState.Config;
            if (string.IsNullOrWhiteSpace(config.GameId) || string.IsNullOrWhiteSpace(config.PlayerId))
            {
                return;
            }

            await StopTracking();

            _cts = new CancellationTokenSource();
            try
            {
                var options = new CardRequestTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    config.GameId,
                    config.PlayerId);

                _subscription = await _signalClient.SubscribeAsync(options, _signalHandler, _cts.Token);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CardRequestSystem] Failed to subscribe to request events: {ex.Message}");
            }
        }

        private async UniTask StopTracking()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _subscription?.Dispose();
            _subscription = null;

            _signalHandler.ResetState();
            await UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
            _subscription?.Dispose();
        }
    }
}
