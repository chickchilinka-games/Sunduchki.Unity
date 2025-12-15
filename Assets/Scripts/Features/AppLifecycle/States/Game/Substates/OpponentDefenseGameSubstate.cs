using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.Lobby.Services;
using Modules.TurnSystem.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public class OpponentDefenseGameSubstate : GameSubstateBase
    {
        private readonly TurnSequenceService _turnSequenceService;
        private readonly ICardRequestService _cardRequestService;

        public OpponentDefenseGameSubstate(
            TurnSequenceService turnSequenceService,
            ICardRequestService cardRequestService,
            LobbyService lobbyService)
            : base(lobbyService)
        {
            _turnSequenceService = turnSequenceService;
            _cardRequestService = cardRequestService;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            var localId = GetLocalPlayerId();
            var awaitedState = _cardRequestService.Current;
            if (!IsAwaitingDefense(localId, awaitedState))
            {
                return Transition.GoTo<WaitTurnGameSubstate>();
            }

            var awaitedSequence = awaitedState.Sequence;

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

                var current = _cardRequestService.Current;
                if (current.Sequence > awaitedSequence)
                {
                    if (IsAwaitingDefense(localId, current))
                    {
                        awaitedSequence = current.Sequence;
                    }
                    else
                    {
                        return Transition.GoTo<WaitTurnGameSubstate>();
                    }
                }
                else if (!IsAwaitingDefense(localId, current))
                {
                    return Transition.GoTo<WaitTurnGameSubstate>();
                }

                await UniTask.WhenAny(
                    _cardRequestService.State.Skip(1).FirstAsync(token).AsUniTask(),
                    _turnSequenceService.State.Skip(1).FirstAsync(token).AsUniTask(),
                    LobbyService.GameEnded.FirstAsync(token).AsUniTask());
            }

            return Transition.GoBack();
        }

        private bool IsAwaitingDefense(string localId, CardRequestState state)
        {
            if (string.IsNullOrWhiteSpace(localId) || !state.HasEvent)
            {
                return false;
            }

            var evt = state.LastEvent;
            return evt.EventType == CardRequestEventType.Requested &&
                   string.Equals(evt.AskerId, localId, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLocalTurn()
        {
            var turnState = _turnSequenceService.State.CurrentValue;
            return !string.IsNullOrWhiteSpace(turnState.CurrentPlayerId) && turnState.IsLocalTurn;
        }
    }
}
