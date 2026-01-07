using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.SignalR;
using UnityEngine;

namespace Modules.CardRequestSystem.Services
{
    public class SignalRCardRequestCommandClient : ICardRequestCommandClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private ISignalRConnection _connection;

        public SignalRCardRequestCommandClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async UniTask ConnectAsync(CardRequestCommandOptions options, CancellationToken cancellationToken = default)
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

        public async UniTask AskAsync(CardRequestAskPayload payload, CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                Debug.LogWarning("[CardRequestSystem] Ask called without an active SignalR connection.");
                return;
            }

            await _connection.InvokeAsync("Ask", new AskRequestDto
            {
                GameId = payload.GameId,
                AskerId = payload.AskerId,
                TargetId = payload.TargetId,
                Rank = payload.Rank
            }, cancellationToken);
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
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
