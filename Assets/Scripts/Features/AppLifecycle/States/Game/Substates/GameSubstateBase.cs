using System;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using UniState;

namespace Features.AppLifecycle.States.Game.Substates
{
    public abstract class GameSubstateBase : StateBase
    {
        protected LobbyService LobbyService { get; }
        protected DefenseDecisionService DefenseDecisionService { get; }

        protected GameSubstateBase(LobbyService lobbyService, DefenseDecisionService defenseDecisionService = null)
        {
            LobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            DefenseDecisionService = defenseDecisionService;
        }

        protected string GetLocalPlayerId()
        {
            var configId = LobbyService.StateContext?.Data.PlayerId;
            if (!string.IsNullOrWhiteSpace(configId))
            {
                return configId;
            }

            var info = LobbyService.GetLocalPlayer();
            return info.Id ?? string.Empty;
        }

        protected bool IsGameEnded()
        {
            return LobbyService.State.CurrentValue.Status == LobbyStatus.Ended;
        }

        protected bool HasDefensePrompt(string playerId)
        {
            if (DefenseDecisionService == null)
            {
                return false;
            }

            var prompt = DefenseDecisionService.CurrentPrompt;
            if (prompt == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return true;
            }

            return string.Equals(prompt.TargetId, playerId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
