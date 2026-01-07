using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Model;
using Modules.SignalR.Config;
using Modules.TurnSystem.Data;
using Modules.TurnSystem.Interfaces;
using Modules.TurnSystem.Services;
using R3;
using Zenject;

namespace Modules.TurnSystem.Rules
{
    public class TrackTurnOnLobbyConnectRule : IInitializable, IDisposable
    {
        private readonly LobbyStateContext _lobbyState;
        private readonly TurnSequenceService _turnService;
        private readonly ITurnSignalClient _signalClient;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _trackingCts;
        private IDisposable _subscription;

        public TrackTurnOnLobbyConnectRule(
            LobbyStateContext lobbyState,
            TurnSequenceService turnService,
            ITurnSignalClient signalClient,
            IGameHubConfigProvider configProvider)
        {
            _lobbyState = lobbyState;
            _turnService = turnService;
            _signalClient = signalClient;
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
                case LobbyStatus.Ended:
                case LobbyStatus.Waiting:
                case LobbyStatus.Idle:
                case LobbyStatus.Joining:
                case LobbyStatus.Connecting:
                    StopTracking().Forget();
                    break;
            }
        }

        private async UniTaskVoid BeginTracking()
        {
            var lobbyConfig = _lobbyState.Data;
            if (string.IsNullOrWhiteSpace(lobbyConfig.GameId) || string.IsNullOrWhiteSpace(lobbyConfig.PlayerId))
            {
                return;
            }

            await StopTracking();

            _turnService.SetLocalPlayer(lobbyConfig.PlayerId);

            _trackingCts = new CancellationTokenSource();
            try
            {
                var options = new TurnTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    lobbyConfig.GameId,
                    lobbyConfig.PlayerId);

                _subscription = await _signalClient.SubscribeAsync(options, OnTurnAdvanced, _trackingCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[TurnSystem] Failed to start tracking turns: {ex.Message}");
            }
        }

        private async UniTask StopTracking()
        {
            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _trackingCts = null;

            _subscription?.Dispose();
            _subscription = null;

            _turnService.Reset();
            await UniTask.CompletedTask;
        }

        private void OnTurnAdvanced(string playerId)
        {
            _turnService.AdvanceTo(playerId);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _subscription?.Dispose();
        }
    }
}
