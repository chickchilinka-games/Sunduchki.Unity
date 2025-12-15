using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.SignalR;

namespace Modules.CardRequestSystem.Services
{
    public class SignalRCardRequestClient : ICardRequestSignalClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;

        public SignalRCardRequestClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async UniTask<IDisposable> SubscribeAsync(
            CardRequestTrackingOptions options,
            ICardRequestSignalListener listener,
            CancellationToken cancellationToken = default)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            RegisterHandlers(connection, listener);

            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = options.GameId,
                PlayerId = options.PlayerId
            }, cancellationToken);

            return new Subscription(connection);
        }

        private static void RegisterHandlers(ISignalRConnection connection, ICardRequestSignalListener listener)
        {
            connection.On<string, string, string>("CardsRequested", (from, target, rank) =>
            {
                listener.OnCardsRequested(from, target, rank);
            });

            connection.On<string, string, string, int>("CardsTransferred", (from, to, rank, count) =>
            {
                listener.OnCardsTransferred(from, to, rank, count);
            });

            connection.On<string, string, string>("NoCardsResponse", (from, target, rank) =>
            {
                listener.OnNoCardsResponse(from, target, rank);
            });

            connection.On<string, string, string, List<string>>("DefenseDecisionRequested", (asker, target, rank, options) =>
            {
                listener.OnDefenseDecisionRequested(asker, target, rank, options ?? new List<string>());
            });
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ISignalRConnection _connection;
            private bool _disposed;

            public Subscription(ISignalRConnection connection)
            {
                _connection = connection;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _connection.StopAsync().Forget();
                _connection.Dispose();
            }
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }
    }
}
