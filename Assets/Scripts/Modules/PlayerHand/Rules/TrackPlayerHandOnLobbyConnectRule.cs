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
        private CancellationTokenSource _resetCts;
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
            UnityEngine.Debug.Log($"[PlayerHand] Lobby state changed: status={state.Status}, started={state.Started}");
            switch (state.Status)
            {
                case LobbyStatus.Started:
                    BeginTracking().Forget();
                    break;
                default:
                    StopTracking(ShouldDelayReset(state), state.Status == LobbyStatus.Ended).Forget();
                    break;
            }
        }

        private async UniTaskVoid BeginTracking()
        {
            var config = _lobbyState.Data;
            if (string.IsNullOrWhiteSpace(config.GameId) || string.IsNullOrWhiteSpace(config.PlayerId))
            {
                UnityEngine.Debug.LogWarning("[PlayerHand] Cannot start tracking: missing game or player id.");
                return;
            }

            await StopTracking(delayReset: false, keepState: false);

            _cts = new CancellationTokenSource();
            try
            {
                CancelPendingReset();
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

        private async UniTask StopTracking(bool delayReset, bool keepState)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _subscription?.Dispose();
            _subscription = null;

            if (delayReset)
            {
                CancelPendingReset();
                _resetCts = new CancellationTokenSource();
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1.2), cancellationToken: _resetCts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            if (!keepState)
            {
                _signalHandler.ResetState();
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
            _subscription?.Dispose();
            CancelPendingReset();
        }

        private void CancelPendingReset()
        {
            _resetCts?.Cancel();
            _resetCts?.Dispose();
            _resetCts = null;
        }

        private static bool ShouldDelayReset(LobbyState state)
        {
            return false;
        }
    }
}
