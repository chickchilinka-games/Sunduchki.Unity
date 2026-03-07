using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using UnityEngine;

namespace Modules.BonusSystem.Services
{
    internal class SignalRBonusActionClient : IBonusActionClient, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private string _gameId = string.Empty;
        private string _playerId = string.Empty;
        private bool _connected;

        public SignalRBonusActionClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[BonusSystem] Missing lobby identifiers, bonus tracking skipped.");
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

        public async UniTask UseBonusAsync(string bonusType, string targetPlayerId, CancellationToken cancellationToken = default)
        {
            if (!_connected)
            {
                Debug.LogError("[BonusSystem] Attempted to use bonus without an active SignalR connection.");
                throw new InvalidOperationException("Bonus SignalR connection is not established.");
            }

            if (string.IsNullOrWhiteSpace(_gameId) || string.IsNullOrWhiteSpace(_playerId))
            {
                Debug.LogWarning("[BonusSystem] Bonus session is not configured.");
                return;
            }

            if (string.IsNullOrWhiteSpace(bonusType))
            {
                Debug.LogWarning("[BonusSystem] Bonus type is empty.");
                return;
            }

            await _sharedHubConnection.InvokeAsync("UseBonus", new UseBonusRequestDto
            {
                GameId = _gameId,
                PlayerId = _playerId,
                BonusType = bonusType,
                TargetPlayerId = string.IsNullOrWhiteSpace(targetPlayerId) ? null : targetPlayerId
            }, cancellationToken);
        }

        public void Dispose()
        {
            _connected = false;
            _gameId = string.Empty;
            _playerId = string.Empty;
        }

        private sealed class UseBonusRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
            public string BonusType { get; set; }
            public string TargetPlayerId { get; set; }
        }
    }
}
