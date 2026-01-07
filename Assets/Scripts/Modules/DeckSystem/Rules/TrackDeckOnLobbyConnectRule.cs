using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Model;
using Modules.SignalR.Config;
using R3;
using Zenject;
using UnityEngine;

namespace Modules.DeckSystem.Rules
{
    public class TrackDeckOnLobbyConnectRule : IInitializable, IDisposable
    {
        private readonly LobbyStateContext _lobbyState;
        private readonly IDeckSignalClient _signalClient;
        private readonly IDeckSignalHandler _signalHandler;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _cts;
        private IDisposable _subscription;

        public TrackDeckOnLobbyConnectRule(
            LobbyStateContext lobbyState,
            IDeckSignalClient signalClient,
            IDeckSignalHandler signalHandler,
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
            var config = _lobbyState.Data;
            if (string.IsNullOrWhiteSpace(config.GameId) || string.IsNullOrWhiteSpace(config.PlayerId))
            {
                Debug.LogWarning("[DeckSystem] Cannot start tracking deck: missing game or player id.");
                return;
            }

            await StopTracking();

            if (config.DeckCount.HasValue || config.TotalCards.HasValue)
            {
                _signalHandler.OnDeckConfigured(config.DeckCount, config.TotalCards);
            }

            _cts = new CancellationTokenSource();
            try
            {
                var options = new DeckTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    config.GameId,
                    config.PlayerId);

                _subscription = await _signalClient.SubscribeAsync(options, _signalHandler, _cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DeckSystem] Failed to track deck state: {ex.Message}");
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
