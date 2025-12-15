using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Services;
using Modules.TurnSystem.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public class OpponentTurnGameSubstate : GameSubstateBase
    {
        private readonly TurnSequenceService _turnSequenceService;

        public OpponentTurnGameSubstate(
            TurnSequenceService turnSequenceService,
            DefenseDecisionService defenseDecisionService,
            LobbyService lobbyService)
            : base(lobbyService, defenseDecisionService)
        {
            _turnSequenceService = turnSequenceService;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            var localId = GetLocalPlayerId();

            while (!token.IsCancellationRequested)
            {
                if (IsGameEnded())
                {
                    return Transition.GoTo<ResultsGameSubstate>();
                }

                if (IsLocalTurn())
                {
                    return Transition.GoTo<WaitTurnGameSubstate>();
                }

                if (HasActiveDefensePrompt(localId))
                {
                    return Transition.GoTo<PlayerDefenseGameSubstate>();
                }

                await UniTask.WhenAny(
                    _turnSequenceService.State.Skip(1).FirstAsync(token).AsUniTask(),
                    DefenseDecisionService.Prompt.Skip(1).FirstAsync(token).AsUniTask(),
                    LobbyService.GameEnded.FirstAsync(token).AsUniTask());
            }

            return Transition.GoBack();
        }

        private bool HasActiveDefensePrompt(string localId)
        {
            return HasDefensePrompt(localId);
        }

        private bool IsLocalTurn()
        {
            var state = _turnSequenceService.State.CurrentValue;
            return !string.IsNullOrWhiteSpace(state.CurrentPlayerId) && state.IsLocalTurn;
        }
    }
}
