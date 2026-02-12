using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using Modules.SignalR.Config;
using R3;
using UnityEngine;

namespace Modules.CardRequestSystem.Services
{
    internal class SignalRCardRequestClient : ICardRequestEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly Subject<CardRequestedEvent> _requested = new();
        private readonly Subject<CardRequestTransferredEvent> _transferred = new();
        private readonly Subject<CardRequestDeniedEvent> _denied = new();
        private CancellationTokenSource _cts;
        private IDisposable _subscription;

        public SignalRCardRequestClient(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public Observable<CardRequestedEvent> Requested => _requested;
        public Observable<CardRequestTransferredEvent> Transferred => _transferred;
        public Observable<CardRequestDeniedEvent> Denied => _denied;

        public async UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[CardRequestSystem] Cannot track requests: missing game or player id.");
                return;
            }

            await StopTracking();

            _cts = new CancellationTokenSource();
            try
            {
                var options = new CardRequestTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    session.GameId,
                    session.PlayerId);

                _subscription = await SubscribeAsync(options, _cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardRequestSystem] Failed to subscribe to request events: {ex.Message}");
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
            _cts = null;
            _subscription?.Dispose();
            _subscription = null;
        }

        private async UniTask StopTracking()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _subscription?.Dispose();
            _subscription = null;

            await UniTask.CompletedTask;
        }

        private async UniTask<IDisposable> SubscribeAsync(
            CardRequestTrackingOptions options,
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
            connection.On<CardsRequestedDto>("CardsRequested", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _requested.OnNext(new CardRequestedEvent(
                    payload.FromPlayerId,
                    payload.TargetPlayerId,
                    payload.Rank));
            });

            connection.On<CardsTransferredDto>("CardsTransferred", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                var mapped = new List<CardTransferCardData>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var cards = payload.Cards ?? Array.Empty<TransferCardDto>();
                foreach (var card in cards)
                {
                    if (string.IsNullOrWhiteSpace(card?.Rank) || string.IsNullOrWhiteSpace(card?.Suit))
                    {
                        continue;
                    }

                    var key = $"{card.Rank}:{card.Suit}";
                    if (seen.Add(key))
                    {
                        mapped.Add(new CardTransferCardData(card.Rank, card.Suit));
                    }
                }

                if (mapped.Count == 0)
                {
                    return;
                }

                _transferred.OnNext(new CardRequestTransferredEvent(
                    payload.PlayerId,
                    payload.Destination,
                    mapped));
            });

            connection.On<NoCardsResponseDto>("NoCardsResponse", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _denied.OnNext(new CardRequestDeniedEvent(
                    payload.FromPlayerId,
                    payload.TargetPlayerId,
                    payload.Rank));
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
            public string PlayerId { get; set; }
            public string Destination { get; set; }
            public TransferCardDto[] Cards { get; set; }
        }

        private sealed class TransferCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class NoCardsResponseDto
        {
            public string FromPlayerId { get; set; }
            public string TargetPlayerId { get; set; }
            public string Rank { get; set; }
        }
    }
}

