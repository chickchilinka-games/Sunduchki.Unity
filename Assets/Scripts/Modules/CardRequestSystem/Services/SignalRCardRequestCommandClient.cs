using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using UnityEngine;

namespace Modules.CardRequestSystem.Services
{
    internal class SignalRCardRequestCommandClient : ICardRequestCommandClient, ILobbyConnectionHandler
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private string _gameId = string.Empty;
        private string _playerId = string.Empty;
        private bool _connected;

        public SignalRCardRequestCommandClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                return UniTask.CompletedTask;
            }

            _connected = true;
            _gameId = session.GameId ?? string.Empty;
            _playerId = session.PlayerId ?? string.Empty;
            return UniTask.CompletedTask;
        }

        public UniTask DisconnectAsync()
        {
            _connected = false;
            _gameId = string.Empty;
            _playerId = string.Empty;
            return UniTask.CompletedTask;
        }

        public async UniTask AskAsync(string rank, string targetPlayerId, CancellationToken cancellationToken = default)
        {
            if (!_connected)
            {
                Debug.LogWarning("[CardRequestSystem] Ask called without an active SignalR connection.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_gameId) || string.IsNullOrWhiteSpace(_playerId))
            {
                Debug.LogWarning("[CardRequestSystem] Ask called without game/player session.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rank) || string.IsNullOrWhiteSpace(targetPlayerId))
            {
                Debug.LogWarning("[CardRequestSystem] Invalid ask request.");
                return;
            }

            await _sharedHubConnection.InvokeAsync("Ask", new AskRequestDto
            {
                GameId = _gameId,
                AskerId = _playerId,
                TargetId = targetPlayerId,
                Rank = rank
            }, cancellationToken);
        }

        private sealed class AskRequestDto
        {
            public string GameId { get; set; }
            public string AskerId { get; set; }
            public string TargetId { get; set; }
            public string Rank { get; set; }
        }
    }
}
