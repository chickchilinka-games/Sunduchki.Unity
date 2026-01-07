using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Interfaces;
using Modules.Lobby.Services;
using Modules.TurnSystem.Services;

namespace Features.PlayerHandSystemImpl.Providers
{
    public sealed class SingleOpponentSelector : ITargetPlayerSelector
    {
        private readonly LobbyService _lobbyService;
        private readonly TurnSequenceService _turnService;

        public SingleOpponentSelector(LobbyService lobbyService, TurnSequenceService turnService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
        }

        public UniTask<string> SelectAsync(CancellationToken cancellationToken = default)
        {
            var players = _lobbyService.Players?.CurrentValue;
            if (players == null)
            {
                return UniTask.FromResult(ResolveFromTurnState(string.Empty));
            }

            var localId = _lobbyService.StateContext.Data.PlayerId ?? string.Empty;
            foreach (var player in players)
            {
                if (string.IsNullOrWhiteSpace(player.Id))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(localId) &&
                    string.Equals(player.Id, localId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!player.IsLocal || string.IsNullOrWhiteSpace(localId))
                {
                    return UniTask.FromResult(player.Id);
                }
            }

            return UniTask.FromResult(ResolveFromTurnState(localId));
        }

        private string ResolveFromTurnState(string localId)
        {
            var turnState = _turnService.State.CurrentValue;
            var previous = turnState.PreviousPlayerId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(previous) &&
                !string.Equals(previous, localId, StringComparison.Ordinal))
            {
                return previous;
            }

            return string.Empty;
        }
    }
}
