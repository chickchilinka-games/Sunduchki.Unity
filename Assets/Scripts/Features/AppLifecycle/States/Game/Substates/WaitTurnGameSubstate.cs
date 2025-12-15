using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Services;
using Modules.TurnSystem.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public class WaitTurnGameSubstate : GameSubstateBase
    {
        private readonly TurnSequenceService _turnSequenceService;
        public WaitTurnGameSubstate(
            TurnSequenceService turnSequenceService,
            LobbyService lobbyService,
            DefenseDecisionService defenseDecisionService)
            : base(lobbyService, defenseDecisionService)
        {
            _turnSequenceService = turnSequenceService;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (IsGameEnded())
                {
                    return Transition.GoTo<ResultsGameSubstate>();
                }

                if (HasActiveDefensePrompt())
                {
                    return Transition.GoTo<PlayerDefenseGameSubstate>();
                }

                var turnState = _turnSequenceService.State.CurrentValue;
                if (!string.IsNullOrWhiteSpace(turnState.CurrentPlayerId))
                {
                    return turnState.IsLocalTurn
                        ? Transition.GoTo<PlayerTurnGameSubstate>()
                        : Transition.GoTo<OpponentTurnGameSubstate>();
                }

                await UniTask.WhenAny(
                    _turnSequenceService.State.Skip(1).FirstAsync(token).AsUniTask(),
                    DefenseDecisionService.Prompt.Skip(1).FirstAsync(token).AsUniTask(),
                    LobbyService.GameEnded.FirstAsync(token).AsUniTask());
            }

            return Transition.GoBack();
        }

        private bool HasActiveDefensePrompt()
        {
            var localId = GetLocalPlayerId();
            return HasDefensePrompt(localId);
        }
    }
}
