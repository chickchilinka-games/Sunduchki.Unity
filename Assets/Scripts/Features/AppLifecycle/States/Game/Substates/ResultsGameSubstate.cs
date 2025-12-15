using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public class ResultsGameSubstate : GameSubstateBase
    {
        public ResultsGameSubstate(LobbyService lobbyService)
            : base(lobbyService)
        { }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            if (!IsGameEnded())
            {
                await LobbyService.GameEnded.FirstAsync(token);
            }

            return Transition.GoBack();
        }
    }
}
