using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;
using Modules.SignalR;

namespace Modules.PlayerHand.Services
{
    public class SignalRPlayerHandClient : IPlayerHandSignalClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;

        public SignalRPlayerHandClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async UniTask<IDisposable> SubscribeAsync(
            PlayerHandTrackingOptions options,
            IPlayerHandSignalListener listener,
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

        private static void RegisterHandlers(ISignalRConnection connection, IPlayerHandSignalListener listener)
        {
            connection.On<string, HandCardDto[]>("PlayerHandUpdated", (playerId, cards) =>
            {
                var mapped = new List<StandardCardData>();
                if (cards != null)
                {
                    foreach (var dto in cards)
                    {
                        mapped.Add(new StandardCardData(dto.Rank, dto.Suit));
                    }
                }

                listener.OnStandardSnapshot(playerId, mapped);
            });

            connection.On<string, string, string>("DrewStandardFromDeck", (playerId, rank, suit) =>
            {
                listener.OnStandardCardAdded(playerId, new StandardCardData(rank, suit));
            });

            connection.On<string, string>("BonusCardAddedToHand", (playerId, bonusType) =>
            {
                listener.OnBonusCardAdded(playerId, new BonusCardData(bonusType));
            });

            connection.On<string, string>("BonusCardRemovedFromHand", (playerId, bonusType) =>
            {
                listener.OnBonusCardRemoved(playerId, new BonusCardData(bonusType));
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

        [Serializable]
        private sealed class HandCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
        }
    }
}
