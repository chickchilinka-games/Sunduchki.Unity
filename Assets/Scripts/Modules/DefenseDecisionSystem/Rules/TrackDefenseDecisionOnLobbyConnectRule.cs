using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Data;
using Modules.Lobby.Model;
using Modules.SignalR.Config;
using R3;
using UnityEngine;
using Zenject;

namespace Modules.DefenseDecisionSystem.Rules
{
    public sealed class TrackDefenseDecisionOnLobbyConnectRule : IInitializable, IDisposable
    {
        private readonly LobbyStateContext _lobbyState;
        private readonly IDefenseDecisionClient _client;
        private readonly DefenseDecisionService _service;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly CompositeDisposable _subscriptions = new();
        private CancellationTokenSource _cts;

        public TrackDefenseDecisionOnLobbyConnectRule(
            LobbyStateContext lobbyState,
            IDefenseDecisionClient client,
            DefenseDecisionService service,
            IGameHubConfigProvider configProvider)
        {
            _lobbyState = lobbyState ?? throw new ArgumentNullException(nameof(lobbyState));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public void Initialize()
        {
            _lobbyState.State
                .Subscribe(OnLobbyStateChanged)
                .AddTo(_subscriptions);
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
                Debug.LogWarning("[DefenseDecision] Cannot track defense decisions without lobby identifiers.");
                return;
            }

            await StopTracking();

            _service.Configure(config.GameId, config.PlayerId);

            _cts = new CancellationTokenSource();
            try
            {
                var options = new DefenseDecisionConnectionOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    config.GameId,
                    config.PlayerId);

                await _client.ConnectAsync(options, _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DefenseDecision] Failed to prepare defense decision client: {ex.Message}");
            }
        }

        private async UniTask StopTracking()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            await _client.DisconnectAsync();
            _service.Reset();
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
