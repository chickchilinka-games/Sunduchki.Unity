using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public class PlayerDefenseGameSubstate : GameSubstateBase
    {
        public PlayerDefenseGameSubstate(
            DefenseDecisionService defenseDecisionService,
            LobbyService lobbyService)
            : base(lobbyService, defenseDecisionService)
        {
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            var localId = GetLocalPlayerId();
            if (!HasActivePrompt(localId))
            {
                return Transition.GoTo<WaitTurnGameSubstate>();
            }

            while (!token.IsCancellationRequested)
            {
                if (IsGameEnded())
                {
                    return Transition.GoTo<ResultsGameSubstate>();
                }

                if (!HasActivePrompt(localId))
                {
                    return Transition.GoTo<WaitTurnGameSubstate>();
                }

                await UniTask.WhenAny(
                    DefenseDecisionService.Prompt.Skip(1).FirstAsync(token).AsUniTask(),
                    LobbyService.GameEnded.FirstAsync(token).AsUniTask());
            }

            return Transition.GoBack();
        }

        private bool HasActivePrompt(string localId)
        {
            return HasDefensePrompt(localId);
        }
    }
}
