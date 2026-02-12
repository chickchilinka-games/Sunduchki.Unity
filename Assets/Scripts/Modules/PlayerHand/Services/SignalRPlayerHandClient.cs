using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;
using Modules.SignalR;
using Modules.SignalR.Config;
using R3;
using UnityEngine;

namespace Modules.PlayerHand.Services
{
    internal class SignalRPlayerHandClient : IPlayerHandEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly Subject<PlayerHandSnapshotEvent> _standardSnapshot = new();
        private readonly Subject<PlayerHandStandardCardEvent> _standardCardAdded = new();
        private readonly Subject<PlayerHandStandardCardEvent> _standardCardRemoved = new();
        private readonly Subject<PlayerHandBonusCardEvent> _bonusCardAdded = new();
        private readonly Subject<PlayerHandBonusCardEvent> _bonusCardRemoved = new();
        private readonly Subject<CardsReceivedEvent> _cardsReceived = new();
        private CancellationTokenSource _cts;
        private IDisposable _subscription;

        public SignalRPlayerHandClient(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public Observable<PlayerHandSnapshotEvent> StandardSnapshot => _standardSnapshot;
        public Observable<PlayerHandStandardCardEvent> StandardCardAdded => _standardCardAdded;
        public Observable<PlayerHandStandardCardEvent> StandardCardRemoved => _standardCardRemoved;
        public Observable<PlayerHandBonusCardEvent> BonusCardAdded => _bonusCardAdded;
        public Observable<PlayerHandBonusCardEvent> BonusCardRemoved => _bonusCardRemoved;
        public Observable<CardsReceivedEvent> CardsReceived => _cardsReceived;

        public async UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[PlayerHand] Cannot start tracking: missing game or player id.");
                return;
            }

            await StopTracking();

            _cts = new CancellationTokenSource();
            try
            {
                var options = new PlayerHandTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    session.GameId,
                    session.PlayerId);

                _subscription = await SubscribeAsync(options, _cts.Token);
                Debug.Log($"[PlayerHand] Subscribed to hand updates for {session.PlayerId}.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerHand] Failed to track hands: {ex.Message}");
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
            PlayerHandTrackingOptions options,
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
            connection.On<PlayerHandUpdatedDto>("PlayerHandUpdated", payload =>
            {
                var mapped = new List<StandardCardData>();
                var cards = payload?.Cards;
                if (cards != null)
                {
                    foreach (var dto in cards)
                    {
                        if (string.IsNullOrWhiteSpace(dto?.Rank) || string.IsNullOrWhiteSpace(dto?.Suit))
                        {
                            continue;
                        }

                        mapped.Add(new StandardCardData(dto.Rank, dto.Suit));
                    }
                }

                var playerId = payload?.PlayerId ?? string.Empty;
                _standardSnapshot.OnNext(new PlayerHandSnapshotEvent(playerId, mapped));
            });

            connection.On<BonusCardChangedDto>("BonusCardAddedToHand", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _bonusCardAdded.OnNext(new PlayerHandBonusCardEvent(
                    payload.PlayerId,
                    new BonusCardData(payload.BonusType)));
            });

            connection.On<BonusCardChangedDto>("BonusCardRemovedFromHand", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _bonusCardRemoved.OnNext(new PlayerHandBonusCardEvent(
                    payload.PlayerId,
                    new BonusCardData(payload.BonusType)));
            });

            connection.On<CardsReceivedDto>("CardsReceived", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                var standard = new List<StandardCardData>();
                var bonus = new List<BonusCardData>();
                if (payload.Cards != null)
                {
                    foreach (var card in payload.Cards)
                    {
                        if (!string.IsNullOrWhiteSpace(card?.BonusType))
                        {
                            bonus.Add(new BonusCardData(card.BonusType));
                        }
                        else if (!string.IsNullOrWhiteSpace(card?.Rank) && !string.IsNullOrWhiteSpace(card?.Suit))
                        {
                            standard.Add(new StandardCardData(card.Rank, card.Suit));
                        }
                    }
                }

                _cardsReceived.OnNext(new CardsReceivedEvent(
                    payload.PlayerId,
                    payload.Source ?? string.Empty,
                    standard,
                    bonus));
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
        private sealed class BonusCardChangedDto
        {
            public string PlayerId { get; set; }
            public string BonusType { get; set; }
        }

        [Serializable]
        private sealed class CardsReceivedDto
        {
            public string PlayerId { get; set; }
            public string Source { get; set; }
            public TransferCardDto[] Cards { get; set; }
        }

        [Serializable]
        private sealed class TransferCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
            public string BonusType { get; set; }
        }
    }
}

