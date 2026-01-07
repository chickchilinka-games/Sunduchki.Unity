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
            connection.On<CardsRequestedDto>("CardsRequested", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnCardsRequested(payload.FromPlayerId, payload.TargetPlayerId, payload.Rank);
            });

            connection.On<CardsTransferredDto>("CardsTransferred", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnCardsTransferred(payload.FromPlayerId, payload.ToPlayerId, payload.Rank, payload.Count);
            });

            connection.On<NoCardsResponseDto>("NoCardsResponse", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnNoCardsResponse(payload.FromPlayerId, payload.TargetPlayerId, payload.Rank);
            });

            connection.On<DefenseDecisionRequestedDto>("DefenseDecisionRequested", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnDefenseDecisionRequested(
                    payload.AskerId,
                    payload.TargetPlayerId,
                    payload.Rank,
                    payload.DefenseOptions ?? new List<string>());
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

        private sealed class CardsRequestedDto
        {
            public string FromPlayerId { get; set; }
            public string TargetPlayerId { get; set; }
            public string Rank { get; set; }
        }

        private sealed class CardsTransferredDto
        {
            public string FromPlayerId { get; set; }
            public string ToPlayerId { get; set; }
            public string Rank { get; set; }
            public int Count { get; set; }
        }

        private sealed class NoCardsResponseDto
        {
            public string FromPlayerId { get; set; }
            public string TargetPlayerId { get; set; }
            public string Rank { get; set; }
        }

        private sealed class DefenseDecisionRequestedDto
        {
            public string AskerId { get; set; }
            public string TargetPlayerId { get; set; }
            public string Rank { get; set; }
            public List<string> DefenseOptions { get; set; }
        }
    }
}
