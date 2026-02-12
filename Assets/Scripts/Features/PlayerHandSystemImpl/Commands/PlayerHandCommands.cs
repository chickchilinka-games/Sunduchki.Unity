using System;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Interfaces;
using Modules.CardRequestSystem.Services;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Services;
using Modules.TurnSystem.Services;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.Commands
{
    public sealed class PlayerHandCommands : IPlayerHandCommands
    {
        private readonly CardRequestService _cardRequestService;
        private readonly DefenseDecisionService _defenseService;
        private readonly LobbyService _lobbyService;
        private readonly TurnSequenceService _turnService;

        public PlayerHandCommands(
            CardRequestService cardRequestService,
            DefenseDecisionService defenseService,
            LobbyService lobbyService,
            TurnSequenceService turnService)
        {
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
        }

        public void HandleRankPress(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var localId = _lobbyService.GetLocalPlayer().Id;
            var prompt = _defenseService.CurrentPrompt;
            if (prompt != null &&
                !string.IsNullOrWhiteSpace(prompt.TargetId) &&
                string.Equals(prompt.TargetId, localId, StringComparison.Ordinal))
            {
                if (string.Equals(NormalizeRank(rank), NormalizeRank(prompt.Rank), StringComparison.OrdinalIgnoreCase))
                {
                    _defenseService
                        .SubmitDecisionAsync(new DefenseDecisionSubmitRequest(false, null))
                        .Forget();
                }

                return;
            }

            if (!_turnService.State.CurrentValue.IsLocalTurn)
            {
                return;
            }

            var opponentId = ResolveOpponentId(localId);
            if (string.IsNullOrWhiteSpace(opponentId))
            {
                Debug.LogWarning("[PlayerHand] Cannot send card request without opponent id.");
                return;
            }

            var rankToSend = ToServerRank(rank);
            if (string.IsNullOrWhiteSpace(rankToSend))
            {
                return;
            }

            _cardRequestService.AskAsync(rankToSend, opponentId).Forget();
        }

        private string ResolveOpponentId(string localId)
        {
            var players = _lobbyService.Players?.CurrentValue;
            if (players == null)
            {
                return ResolveOpponentFromTurnState(localId);
            }

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
                    return player.Id;
                }
            }

            return ResolveOpponentFromTurnState(localId);
        }

        private string ResolveOpponentFromTurnState(string localId)
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

        private static string ToServerRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return string.Empty;
            }

            return rank.Trim().ToLowerInvariant() switch
            {
                "two" => "Two",
                "three" => "Three",
                "four" => "Four",
                "five" => "Five",
                "six" => "Six",
                "seven" => "Seven",
                "eight" => "Eight",
                "nine" => "Nine",
                "ten" => "Ten",
                "jack" => "Jack",
                "queen" => "Queen",
                "king" => "King",
                "ace" => "Ace",
                _ => string.Empty
            };
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank)
                ? string.Empty
                : rank.Trim().ToLowerInvariant();
        }
    }
}
