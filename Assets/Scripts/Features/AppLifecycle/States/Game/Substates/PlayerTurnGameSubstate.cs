using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Services;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Services;
using Modules.TurnSystem.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public class PlayerTurnGameSubstate : GameSubstateBase
    {
        private readonly TurnSequenceService _turnSequenceService;
        private readonly CardRequestService _cardRequestService;

        public PlayerTurnGameSubstate(
            TurnSequenceService turnSequenceService,
            CardRequestService cardRequestService,
            DefenseDecisionService defenseDecisionService,
            LobbyService lobbyService)
            : base(lobbyService, defenseDecisionService)
        {
            _turnSequenceService = turnSequenceService;
            _cardRequestService = cardRequestService;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            var localId = GetLocalPlayerId();
            if (string.IsNullOrWhiteSpace(localId))
            {
                return Transition.GoTo<WaitTurnGameSubstate>();
            }

            while (!token.IsCancellationRequested)
            {
                if (IsGameEnded())
                {
                    return Transition.GoTo<ResultsGameSubstate>();
                }

                if (!IsLocalTurn())
                {
                    return Transition.GoTo<WaitTurnGameSubstate>();
                }

                if (HasActiveDefensePrompt(localId))
                {
                    return Transition.GoTo<PlayerDefenseGameSubstate>();
                }

                if (IsAwaitingOpponentDefense(localId, _cardRequestService.Current))
                {
                    return Transition.GoTo<OpponentDefenseGameSubstate>();
                }

                await UniTask.WhenAny(
                    _turnSequenceService.State.Skip(1).FirstAsync(token).AsUniTask(),
                    _cardRequestService.State.Skip(1).FirstAsync(token).AsUniTask(),
                    DefenseDecisionService.Prompt.Skip(1).FirstAsync(token).AsUniTask(),
                    LobbyService.GameEnded.FirstAsync(token).AsUniTask());
            }

            return Transition.GoBack();
        }

        private bool IsAwaitingOpponentDefense(string localId, CardRequestState requestState)
        {
            if (!requestState.HasEvent)
            {
                return false;
            }

            var evt = requestState.LastEvent;
            return evt.EventType == CardRequestEventType.Requested &&
                   string.Equals(evt.AskerId, localId, StringComparison.OrdinalIgnoreCase);
        }

        private bool HasActiveDefensePrompt(string localId)
        {
            return HasDefensePrompt(localId);
        }

        private bool IsLocalTurn()
        {
            var turnState = _turnSequenceService.State.CurrentValue;
            return !string.IsNullOrWhiteSpace(turnState.CurrentPlayerId) && turnState.IsLocalTurn;
        }
    }
}
