using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Data;
using Modules.BonusSystem.Interfaces;
using Modules.SignalR;
using UnityEngine;

namespace Modules.BonusSystem.Services
{
    public class SignalRBonusActionClient : IBonusActionClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private ISignalRConnection _connection;

        public SignalRBonusActionClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async UniTask ConnectAsync(BonusConnectionOptions options, CancellationToken cancellationToken = default)
        {
            await DisconnectAsync();

            var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = options.GameId,
                PlayerId = options.PlayerId
            }, cancellationToken);

            _connection = connection;
        }

        public async UniTask DisconnectAsync()
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
        }

        public async UniTask UseBonusAsync(BonusUsePayload payload, CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                Debug.LogError("[BonusSystem] Attempted to use bonus without an active SignalR connection.");
                throw new InvalidOperationException("Bonus SignalR connection is not established.");
            }

            await _connection.InvokeAsync("UseBonus", new UseBonusRequestDto
            {
                GameId = payload.GameId,
                PlayerId = payload.PlayerId,
                BonusType = payload.BonusType,
                TargetPlayerId = string.IsNullOrWhiteSpace(payload.TargetPlayerId) ? null : payload.TargetPlayerId
            }, cancellationToken);
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
