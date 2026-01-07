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
            connection.On<PlayerHandUpdatedDto>("PlayerHandUpdated", payload =>
            {
                var mapped = new List<StandardCardData>();
                var cards = payload?.Cards;
                if (cards != null)
                {
                    foreach (var dto in cards)
                    {
                        mapped.Add(new StandardCardData(dto.Rank, dto.Suit));
                    }
                }

                var playerId = payload?.PlayerId ?? string.Empty;
                listener.OnStandardSnapshot(playerId, mapped);
            });

            connection.On<StandardCardDrawnDto>("DrewStandardFromDeck", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnStandardCardAdded(payload.PlayerId, new StandardCardData(payload.Rank, payload.Suit));
            });

            connection.On<BonusCardChangedDto>("BonusCardAddedToHand", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnBonusCardAdded(payload.PlayerId, new BonusCardData(payload.BonusType));
            });

            connection.On<BonusCardChangedDto>("BonusCardRemovedFromHand", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnBonusCardRemoved(payload.PlayerId, new BonusCardData(payload.BonusType));
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

        [Serializable]
        private sealed class PlayerHandUpdatedDto
        {
            public string PlayerId { get; set; }
            public HandCardDto[] Cards { get; set; }
        }

        [Serializable]
        private sealed class StandardCardDrawnDto
        {
            public string PlayerId { get; set; }
            public string Rank { get; set; }
            public string Suit { get; set; }
        }

        [Serializable]
        private sealed class BonusCardChangedDto
        {
            public string PlayerId { get; set; }
            public string BonusType { get; set; }
        }
    }
}
