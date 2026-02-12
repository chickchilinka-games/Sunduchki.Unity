using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using Modules.SignalR.Config;
using R3;
using UnityEngine;

namespace Modules.DeckSystem.Services
{
    internal class SignalRDeckClient : IDeckEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly Subject<DeckConfiguredEvent> _configured = new();
        private readonly Subject<DeckCardDrawnEvent> _standardDrawn = new();
        private readonly Subject<DeckBonusCardDrawnEvent> _bonusDrawn = new();
        private readonly Subject<DeckPeekedEvent> _peeked = new();
        private readonly Subject<DeckAdjustedEvent> _adjusted = new();
        private CancellationTokenSource _cts;
        private IDisposable _subscription;

        public SignalRDeckClient(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public Observable<DeckConfiguredEvent> Configured => _configured;
        public Observable<DeckCardDrawnEvent> StandardDrawn => _standardDrawn;
        public Observable<DeckBonusCardDrawnEvent> BonusDrawn => _bonusDrawn;
        public Observable<DeckPeekedEvent> Peeked => _peeked;
        public Observable<DeckAdjustedEvent> Adjusted => _adjusted;

        public async UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[DeckSystem] Cannot start tracking deck: missing game or player id.");
                return;
            }

            await StopTracking();

            _cts = new CancellationTokenSource();
            try
            {
                var options = new DeckTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    session.GameId,
                    session.PlayerId);

                _subscription = await SubscribeAsync(options, _cts.Token);
                Debug.Log($"[DeckSystem] Subscribed to deck updates for {session.PlayerId}.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeckSystem] Failed to track deck state: {ex.Message}");
            }
        }

        public async UniTask DisconnectAsync()
        {
            await StopTracking();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _subscription?.Dispose();
        }

        private async UniTask StopTracking()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _subscription?.Dispose();
            _subscription = null;
        }

        private async UniTask<IDisposable> SubscribeAsync(
            DeckTrackingOptions options,
            CancellationToken cancellationToken = default)
        {
            var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            RegisterHandlers(connection);

            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = options.GameId,
                PlayerId = options.PlayerId
            }, cancellationToken);

            return new Subscription(connection);
        }

        private void RegisterHandlers(ISignalRConnection connection)
        {
            connection.On<DeckConfiguredDto>("DeckConfigured", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _configured.OnNext(new DeckConfiguredEvent(payload.RemainingCards, payload.TotalCards));
            });

            connection.On<BonusCardDrawnDto>("DrewBonusFromDeck", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _bonusDrawn.OnNext(new DeckBonusCardDrawnEvent(payload.PlayerId, payload.BonusType));
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
                _peeked.OnNext(new DeckPeekedEvent(payload.PlayerId, cards));
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

                _peeked.OnNext(new DeckPeekedEvent(payload.PlayerId, cards));
            });

            connection.On<DeckAdjustedDto>("DeckAdjusted", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _adjusted.OnNext(new DeckAdjustedEvent(payload.Delta));
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

