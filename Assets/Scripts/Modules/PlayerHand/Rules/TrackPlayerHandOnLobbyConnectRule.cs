using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Model;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;
using Modules.SignalR.Config;
using R3;
using Zenject;

namespace Modules.PlayerHand.Rules
{
    public class TrackPlayerHandOnLobbyConnectRule : IInitializable, IDisposable
    {
        private readonly LobbyStateContext _lobbyState;
        private readonly IPlayerHandSignalClient _signalClient;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly IPlayerHandSignalHandler _signalHandler;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _cts;
        private IDisposable _subscription;

        public TrackPlayerHandOnLobbyConnectRule(
            LobbyStateContext lobbyState,
            IPlayerHandSignalClient signalClient,
            IGameHubConfigProvider configProvider,
            IPlayerHandSignalHandler signalHandler)
        {
            _lobbyState = lobbyState;
            _signalClient = signalClient;
            _configProvider = configProvider;
            _signalHandler = signalHandler;
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
            var config = _lobbyState.Data;
            if (string.IsNullOrWhiteSpace(config.GameId) || string.IsNullOrWhiteSpace(config.PlayerId))
            {
                return;
            }

            await StopTracking();

            _cts = new CancellationTokenSource();
            try
            {
                var options = new PlayerHandTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    config.GameId,
                    config.PlayerId);

                _subscription = await _signalClient.SubscribeAsync(options, _signalHandler, _cts.Token);
                UnityEngine.Debug.Log($"[PlayerHand] Subscribed to hand updates for {config.PlayerId}.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[PlayerHand] Failed to track hands: {ex.Message}");
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
