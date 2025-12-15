using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Data;
using Modules.BonusSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Model;
using Modules.SignalR.Config;
using R3;
using Zenject;
using UnityEngine;

namespace Modules.BonusSystem.Rules
{
    public class TrackBonusActionsOnLobbyConnectRule : IInitializable, IDisposable
    {
        private readonly LobbyStateContext _lobbyState;
        private readonly IBonusActionClient _client;
        private readonly IBonusActionService _service;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _cts;

        public TrackBonusActionsOnLobbyConnectRule(
            LobbyStateContext lobbyState,
            IBonusActionClient client,
            IBonusActionService service,
            IGameHubConfigProvider configProvider)
        {
            _lobbyState = lobbyState;
            _client = client;
            _service = service;
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
                Debug.LogWarning("[BonusSystem] Missing lobby identifiers, bonus tracking skipped.");
                return;
            }

            await StopTracking();

            _service.Configure(config.GameId, config.PlayerId);

            _cts = new CancellationTokenSource();
            try
            {
                var options = new BonusConnectionOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    config.GameId,
                    config.PlayerId);

                await _client.ConnectAsync(options, _cts.Token);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[BonusSystem] Failed to prepare bonus actions: {ex.Message}");
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
            _disposables.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
