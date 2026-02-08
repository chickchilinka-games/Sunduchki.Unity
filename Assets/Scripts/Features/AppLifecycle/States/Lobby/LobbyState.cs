using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.Services;
using Features.AppLifecycle.States.Game;
using Features.AppLifecycle.States.Home;
using Features.LobbyImpl.View;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Modules.SignalR.Config;
using Modules.Lobby.Services;
using R3;
using UniState;
using UnityEngine;

namespace Features.AppLifecycle.States.Lobby
{
    public class LobbyState : StateBase
    {
        private readonly IGameHubConfigProvider _hubConfigProvider;
        private readonly LobbyService _lobbyService;
        private readonly LobbyFlowService _lobbyFlowService;
        private readonly WindowSystem _windowSystem;

        public LobbyState(
            IGameHubConfigProvider hubConfigProvider,
            LobbyService lobbyService,
            LobbyFlowService lobbyFlowService,
            WindowSystem windowSystem)
        {
            _hubConfigProvider = hubConfigProvider;
            _lobbyService = lobbyService;
            _lobbyFlowService = lobbyFlowService;
            _windowSystem = windowSystem;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            await ConnectLobbyAsync(token);
            if (_lobbyService.State.CurrentValue.Started)
                return Transition.GoTo<GameState>();
            
            var exitTask = _lobbyFlowService.ExitRequested.FirstAsync(token).AsUniTask();
            var startedTask = _lobbyService.GameStarted.FirstAsync(token).AsUniTask();
            
            await _windowSystem.ShowWindowAsync<LobbyContent>(nameof(BlockerWindowTemplate));

            var (completed, _, _) = await UniTask.WhenAny(exitTask, startedTask);

            if (completed == 0)
            {
                await ExitLobbyAsync(token);
                return Transition.GoTo<HomeState>();
            }

            return Transition.GoTo<GameState>();
        }

        private async UniTask ConnectLobbyAsync(CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_lobbyService.StateContext.Data.GameId) ||
                string.IsNullOrWhiteSpace(_lobbyService.StateContext.Data.PlayerId))
            {
                Debug.LogWarning("[LobbyState] Lobby config is incomplete, skipping SignalR connect.");
                return;
            }

            var hubUri = _hubConfigProvider?.GetHubUri();
            if (hubUri == null)
            {
                Debug.LogWarning("[LobbyState] Hub URI is not configured.");
                return;
            }
            try
            {
                await _lobbyService.ConnectAsync(hubUri, token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyState] Failed to connect to lobby hub: {ex.Message}");
            }
        }

        private async UniTask ExitLobbyAsync(CancellationToken token)
        {
            await _lobbyService.DisconnectAsync(token);
            await _windowSystem.CloseWindowsWithContentAsync<LobbyContent>();
        }

    }
}
