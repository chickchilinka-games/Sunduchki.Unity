using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.Services;
using Features.AppLifecycle.States.Game.View;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using R3;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public class ResultsGameSubstate : GameSubstateBase
    {
        private readonly WindowSystem _windowSystem;
        private readonly GameResultsFlowService _resultsFlowService;

        public ResultsGameSubstate(
            LobbyService lobbyService,
            WindowSystem windowSystem,
            GameResultsFlowService resultsFlowService)
            : base(lobbyService)
        {
            _windowSystem = windowSystem;
            _resultsFlowService = resultsFlowService;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            LobbyGameEndedPayload payload;
            if (!IsGameEnded())
            {
                payload = await LobbyService.GameEnded.FirstAsync(token);
            }
            else
            {
                payload = new LobbyGameEndedPayload(
                    LobbyService.State.CurrentValue.Result,
                    LobbyService.State.CurrentValue.GameEndedReason);
            }

            if (_windowSystem != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
                var isWin = ResolveIsWin();
                var reason = payload.Reason;
                if (string.IsNullOrWhiteSpace(reason) && payload.Result != null &&
                    payload.Result.Players.Count == 0 && isWin)
                {
                    reason = "Opponent left the game.";
                }
                await _windowSystem.ShowWindowAsync<GameEndedContent, GameEndedContentData>(
                    nameof(BlockerWindowTemplate),
                    new GameEndedContentData(isWin, reason));
            }

            await _resultsFlowService.ExitRequested.FirstAsync(token);
            if (_windowSystem != null)
            {
                await _windowSystem.CloseWindowsWithContentAsync<GameEndedContent>();
            }

            return Transition.GoToExit();
        }

        private bool ResolveIsWin()
        {
            var localId = GetLocalPlayerId();
            if (string.IsNullOrWhiteSpace(localId))
            {
                return false;
            }

            var result = LobbyService.State.CurrentValue.Result;
            if (result == null || result.WinnerPlayerIds.Count == 0)
            {
                return false;
            }

            return result.WinnerPlayerIds.Contains(localId);
        }
    }
}
