using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.States.Game;
using Features.AppLifecycle.States.Home.View;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Modules.Lobby.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Home
{
    public class HomeState: StateBase
    {
        private readonly LobbyService _lobbyService;
        private readonly WindowSystem _windowSystem;

        public HomeState(LobbyService lobbyService, WindowSystem windowSystem)
        {
            _lobbyService = lobbyService;
            _windowSystem = windowSystem;
        }


        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            await _windowSystem.ShowWindowAsync<MainMenuContent>(nameof(BlockerWindowTemplate));
            await _lobbyService.GameStarted.FirstAsync(cancellationToken: token);      
            return Transition.GoTo<GameState>();
        }
    }
}