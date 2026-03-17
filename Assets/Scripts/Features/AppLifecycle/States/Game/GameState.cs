using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.States.Game.Substates;
using Features.AppLifecycle.States.Game.View;
using Features.AppLifecycle.States.Home;
using Features.AppLifecycle.Services;
using Features.WindowSystemImpl.Templates;
using Chickchilinka.Window;
using Modules.Lobby.Services;
using Modules.StateMachine.States;
using R3;
using UniState;
using UnityEngine.SceneManagement;

namespace Features.AppLifecycle.States.Game
{
    public class GameState: StateBase
    {
        private readonly ISubStateMachine _subStateMachine;
        private readonly LobbyService _lobbyService;
        private readonly GameMenuFlowService _menuFlowService;

        public GameState(
            ISubStateMachine subStateMachine,
            LobbyService lobbyService,
            GameMenuFlowService menuFlowService)
        {
            _subStateMachine = subStateMachine;
            _lobbyService = lobbyService;
            _menuFlowService = menuFlowService;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            await SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single).ToUniTask(cancellationToken: token);

            var exitTask = _menuFlowService.ExitRequested.Select(_ => true).FirstAsync(token).AsUniTask();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var subStateTask = _subStateMachine.Execute<WaitTurnGameSubstate>(linkedCts.Token);

            var (hasResult, exitRequested) = await UniTask.WhenAny(exitTask, subStateTask);

            if (hasResult && exitRequested)
            {
                linkedCts.Cancel();
                await ExitGameAsync(token);
                return Transition.GoTo<HomeState>();
            }

            if (_lobbyService != null && _lobbyService.State.CurrentValue.Status == Modules.Lobby.Data.LobbyStatus.Ended)
            {
                await _lobbyService.DisconnectAsync(token);
            }

            return Transition.GoBackTo<HomeState>();
        }


        private async UniTask ExitGameAsync(CancellationToken token)
        {
            if (_lobbyService != null)
            {
                await _lobbyService.DisconnectAsync(token);
            }
        }
    }
}
