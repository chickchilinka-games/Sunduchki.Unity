using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using Modules.SignalR.Config;
using UnityEngine;

namespace Modules.BonusSystem.Services
{
    internal class SignalRBonusActionClient : IBonusActionClient, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private ISignalRConnection _connection;
        private string _gameId = string.Empty;
        private string _playerId = string.Empty;

        public SignalRBonusActionClient(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public async UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[BonusSystem] Missing lobby identifiers, bonus tracking skipped.");
                return;
            }

            await DisconnectCoreAsync();

            var connection = _connectionFactory.Create(_configProvider.GetHubUri(), _configProvider.GetAccessToken());
            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = session.GameId,
                PlayerId = session.PlayerId
            }, cancellationToken);

            _connection = connection;
            _gameId = session.GameId ?? string.Empty;
            _playerId = session.PlayerId ?? string.Empty;
        }

        public async UniTask DisconnectAsync()
        {
            await DisconnectCoreAsync();
        }

        public async UniTask UseBonusAsync(string bonusType, string targetPlayerId, CancellationToken cancellationToken = default)
        {
            if (_connection == null)
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

            await _connection.InvokeAsync("UseBonus", new UseBonusRequestDto
            {
                GameId = _gameId,
                PlayerId = _playerId,
                BonusType = bonusType,
                TargetPlayerId = string.IsNullOrWhiteSpace(targetPlayerId) ? null : targetPlayerId
            }, cancellationToken);
        }

        public void Dispose()
        {
            DisconnectCoreAsync().Forget();
        }

        private async UniTask DisconnectCoreAsync()
        {
            var connection = Interlocked.Exchange(ref _connection, null);
            if (connection == null)
            {
                return;
            }

            try
            {
                await connection.StopAsync();
            }
            finally
            {
                connection.Dispose();
            }

            _gameId = string.Empty;
            _playerId = string.Empty;
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
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
