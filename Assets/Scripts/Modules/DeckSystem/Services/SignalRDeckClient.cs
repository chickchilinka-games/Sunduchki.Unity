using System;
using System.Collections.Generic;
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
            connection.On<DeckConfiguredDto>("DeckConfigured", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnDeckConfigured(payload.RemainingCards, payload.TotalCards);
            });

            connection.On<BonusCardDrawnDto>("DrewBonusFromDeck", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnBonusCardDrawn(payload.PlayerId, payload.BonusType);
            });

            connection.On<PeekedAtDeckDto>("PeekedAtDeck", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                var rank = payload.Rank ?? string.Empty;
                var suit = payload.Suit ?? string.Empty;
                var bonus = payload.BonusType ?? string.Empty;
                var cards = new List<DeckPeekCardData>
                {
                    new DeckPeekCardData(rank, suit, bonus)
                };
                listener.OnDeckPeeked(payload.PlayerId, cards);
            });

            connection.On<PeekedAtDecksDto>("PeekedAtDecks", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                var cards = new List<DeckPeekCardData>();
                if (payload.Cards != null)
                {
                    foreach (var card in payload.Cards)
                    {
                        cards.Add(new DeckPeekCardData(
                            card.Rank ?? string.Empty,
                            card.Suit ?? string.Empty,
                            card.BonusType ?? string.Empty));
                    }
                }

                listener.OnDeckPeeked(payload.PlayerId, cards);
            });

            connection.On<DeckAdjustedDto>("DeckAdjusted", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnDeckAdjusted(payload.Delta);
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

        private sealed class DeckConfiguredDto
        {
            public int? RemainingCards { get; set; }
            public int? TotalCards { get; set; }
        }

        private sealed class BonusCardDrawnDto
        {
            public string PlayerId { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class PeekedAtDeckDto
        {
            public string PlayerId { get; set; }
            public string Rank { get; set; }
            public string Suit { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class PeekedAtDecksDto
        {
            public string PlayerId { get; set; }
            public PeekedCardDto[] Cards { get; set; }
        }

        private sealed class PeekedCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class DeckAdjustedDto
        {
            public int Delta { get; set; }
        }
    }
}
