using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.States.Home.View;
using Features.AppLifecycle.States.Lobby;
using Features.AppLifecycle.Services;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using R3;
using UniState;
using UnityEngine.SceneManagement;

namespace Features.AppLifecycle.States.Home
{
    public class HomeState: StateBase
    {
        private readonly WindowSystem _windowSystem;
        private readonly LobbyFlowService _lobbyFlowService;

        public HomeState(WindowSystem windowSystem, LobbyFlowService lobbyFlowService)
        {
            _windowSystem = windowSystem;
            _lobbyFlowService = lobbyFlowService;
        }


        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            await SceneManager.LoadSceneAsync("Home", LoadSceneMode.Single).ToUniTask(cancellationToken: token);
            await _windowSystem.ShowWindowAsync<MainMenuContent>(nameof(BlockerWindowTemplate));
            await _lobbyFlowService.EnterRequested.FirstAsync(cancellationToken: token);
            return Transition.GoTo<LobbyState>();
        }

        public override UniTask Exit(CancellationToken token)
        {
            _windowSystem.CloseWindowsWithContentAsync<MainMenuContent>().Forget();
            return UniTask.CompletedTask;
        }
    }
}
