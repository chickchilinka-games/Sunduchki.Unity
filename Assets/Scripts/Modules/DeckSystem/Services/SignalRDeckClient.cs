using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;
using Modules.SignalR;

namespace Modules.DeckSystem.Services
{
    public class SignalRDeckClient : IDeckSignalClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;

        public SignalRDeckClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async UniTask<IDisposable> SubscribeAsync(
            DeckTrackingOptions options,
            IDeckSignalListener listener,
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

        private static void RegisterHandlers(ISignalRConnection connection, IDeckSignalListener listener)
        {
            connection.On<int?, int?>("DeckConfigured", (remaining, total) =>
            {
                listener.OnDeckConfigured(remaining, total);
            });

            connection.On<string, string, string>("DrewStandardFromDeck", (playerId, rank, suit) =>
            {
                listener.OnStandardCardDrawn(playerId, new DeckCardData(rank, suit));
            });

            connection.On<string, string>("DrewBonusFromDeck", (playerId, bonusType) =>
            {
                listener.OnBonusCardDrawn(playerId, bonusType);
            });

            connection.On<string, string, string, string>("PeekedAtDeck", (playerId, rank, suit, bonusType) =>
            {
                listener.OnDeckPeeked(playerId, new DeckCardData(rank, suit), bonusType);
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
