using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Services;
using Modules.Lobby.Data;
using Modules.Lobby.Model;
using Modules.SignalR.Config;
using R3;
using UnityEngine;
using Zenject;

namespace Features.CardRequestSystemImpl.Rules
{
    public class TrackCardRequestsOnLobbyConnectRule : IInitializable, IDisposable
    {
        private readonly LobbyStateContext _lobbyState;
        private readonly CardRequestTrackingService _trackingService;
        private readonly CardRequestCommandService _commandService;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _cts;
        private bool _isTracking;
        private string _trackedGameId = string.Empty;
        private string _trackedPlayerId = string.Empty;

        public TrackCardRequestsOnLobbyConnectRule(
            LobbyStateContext lobbyState,
            CardRequestTrackingService trackingService,
            CardRequestCommandService commandService,
            IGameHubConfigProvider configProvider)
        {
            _lobbyState = lobbyState ?? throw new ArgumentNullException(nameof(lobbyState));
            _trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
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
                    if (ShouldStartTracking(state))
                    {
                        BeginTracking().Forget();
                    }
                    break;
                default:
                    if (_isTracking)
                    {
                        StopTracking().Forget();
                    }
                    break;
            }
        }

        private async UniTaskVoid BeginTracking()
        {
            var config = _lobbyState.Data;
            if (string.IsNullOrWhiteSpace(config.GameId) || string.IsNullOrWhiteSpace(config.PlayerId))
            {
                Debug.LogWarning("[CardRequestSystem] Missing game/player id, cannot subscribe to request events.");
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

                var commandOptions = new CardRequestCommandOptions(
                    options.HubUri,
                    options.AccessToken,
                    options.GameId,
                    options.PlayerId);

                await _commandService.ConnectAsync(commandOptions, _cts.Token);
                await _trackingService.StartAsync(options, _cts.Token);
                _isTracking = true;
                _trackedGameId = options.GameId ?? string.Empty;
                _trackedPlayerId = options.PlayerId ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardRequestSystem] Failed to subscribe to request events: {ex.Message}");
                _isTracking = false;
                _trackedGameId = string.Empty;
                _trackedPlayerId = string.Empty;
            }
        }

        private async UniTask StopTracking()
        {
            await _trackingService.StopAsync();
            await _commandService.DisconnectAsync();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isTracking = false;
            _trackedGameId = string.Empty;
            _trackedPlayerId = string.Empty;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private bool ShouldStartTracking(LobbyState state)
        {
            var config = _lobbyState.Data;
            if (string.IsNullOrWhiteSpace(config.GameId) || string.IsNullOrWhiteSpace(config.PlayerId))
            {
                return false;
            }

            if (!_isTracking)
            {
                return true;
            }

            return !string.Equals(_trackedGameId, config.GameId, StringComparison.Ordinal) ||
                   !string.Equals(_trackedPlayerId, config.PlayerId, StringComparison.Ordinal);
        }
    }
}
