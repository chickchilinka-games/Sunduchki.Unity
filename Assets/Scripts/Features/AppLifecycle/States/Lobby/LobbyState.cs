using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.Services;
using Features.AppLifecycle.States.Game;
using Features.AppLifecycle.States.Home;
using Features.LobbyImpl.View;
using Features.WindowSystemImpl.Templates;
using Chickchilinka.Window;
using Modules.Lobby.Data;
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
            if (IsGameReady())
                return Transition.GoTo<GameState>();

            var exitRequested = false;
            using var exitSubscription = _lobbyFlowService.ExitRequested.Subscribe(_ => exitRequested = true);

            await _windowSystem.ShowWindowAsync<LobbyContent>(nameof(BlockerWindowTemplate));
            if (IsGameReady())
            {
                await _windowSystem.CloseWindowsWithContentAsync<LobbyContent>();
                return Transition.GoTo<GameState>();
            }

            while (true)
            {
                var readyTask = UniTask.WaitUntil(IsGameReady, cancellationToken: token);
                var exitTask = UniTask.WaitUntil(() => exitRequested, cancellationToken: token);
                var retryDelayTask = UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);

                var (winner, _, _, _) = await UniTask.WhenAny(
                    WaitSignalAsync(exitTask, 0),
                    WaitSignalAsync(readyTask, 1),
                    WaitSignalAsync(retryDelayTask, 2));

                if (winner == 0)
                {
                    await ExitLobbyAsync(token);
                    return Transition.GoTo<HomeState>();
                }

                if (winner == 1)
                {
                    break;
                }

                if (!_lobbyService.IsConnected)
                {
                    await ConnectLobbyAsync(token);
                }
            }

            await _windowSystem.CloseWindowsWithContentAsync<LobbyContent>();
            return Transition.GoTo<GameState>();
        }

        private async UniTask ConnectLobbyAsync(CancellationToken token)
        {
            if (!_lobbyService.TryGetSession(out _, out _))
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

        private bool IsGameStarted()
        {
            var state = _lobbyService.State.CurrentValue;
            return state.Started || state.Status == LobbyStatus.Started;
        }

        private bool IsGameReady()
        {
            return IsGameStarted() && _lobbyService.IsConnected;
        }

        private static async UniTask<int> WaitSignalAsync(UniTask task, int signal)
        {
            await task;
            return signal;
        }

    }
}
