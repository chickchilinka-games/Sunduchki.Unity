using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Interfaces;
using Modules.SignalR;
using R3;
using UnityEngine;

namespace Modules.PlayerHand.Services
{
    internal class SignalRPlayerHandClient : IPlayerHandEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private readonly Subject<PlayerHandSnapshotEvent> _standardSnapshot = new();
        private readonly Subject<PlayerHandStandardCardEvent> _standardCardAdded = new();
        private readonly Subject<PlayerHandStandardCardEvent> _standardCardRemoved = new();
        private readonly Subject<PlayerHandBonusCardEvent> _bonusCardAdded = new();
        private readonly Subject<PlayerHandBonusCardEvent> _bonusCardRemoved = new();
        private readonly Subject<CardsReceivedEvent> _cardsReceived = new();
        private readonly List<IDisposable> _handlerSubscriptions = new();

        public SignalRPlayerHandClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public Observable<PlayerHandSnapshotEvent> StandardSnapshot => _standardSnapshot;
        public Observable<PlayerHandStandardCardEvent> StandardCardAdded => _standardCardAdded;
        public Observable<PlayerHandStandardCardEvent> StandardCardRemoved => _standardCardRemoved;
        public Observable<PlayerHandBonusCardEvent> BonusCardAdded => _bonusCardAdded;
        public Observable<PlayerHandBonusCardEvent> BonusCardRemoved => _bonusCardRemoved;
        public Observable<CardsReceivedEvent> CardsReceived => _cardsReceived;

        public UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[PlayerHand] Cannot start tracking: missing game or player id.");
                return UniTask.CompletedTask;
            }

            ClearHandlers();
            RegisterHandlers();
            Debug.Log($"[PlayerHand] Subscribed to hand updates for {session.PlayerId}.");
            return UniTask.CompletedTask;
        }

        public UniTask DisconnectAsync()
        {
            ClearHandlers();
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            ClearHandlers();
        }

        private void RegisterHandlers()
        {
            ClearHandlers();
            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<PlayerHandUpdatedDto>("PlayerHandUpdated", payload =>
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
                var revision = payload?.Revision ?? 0L;
                _standardSnapshot.OnNext(new PlayerHandSnapshotEvent(playerId, mapped, revision));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<BonusCardChangedDto>("BonusCardAddedToHand", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _bonusCardAdded.OnNext(new PlayerHandBonusCardEvent(
                    payload.PlayerId,
                    new BonusCardData(payload.BonusType)));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<BonusCardChangedDto>("BonusCardRemovedFromHand", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _bonusCardRemoved.OnNext(new PlayerHandBonusCardEvent(
                    payload.PlayerId,
                    new BonusCardData(payload.BonusType)));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<CardsTransferredDto>("CardsTransferred", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                if (string.Equals(payload.Destination, "chest", StringComparison.OrdinalIgnoreCase))
                {
                    // Chest completion is handled via snapshots + completedSet metadata on CardsReceived.
                    // Per-card removals here create races with receive animation of the 4th card.
                    return;
                }

                var playerId = payload.PlayerId ?? string.Empty;
                var eventSeq = payload.EventSeq;
                var completedSet = payload.CompletedSet;
                var completedSetRank = payload.CompletedSetRank ?? string.Empty;
                var cards = payload.Cards ?? Array.Empty<TransferCardDto>();
                foreach (var card in cards)
                {
                    if (string.IsNullOrWhiteSpace(card?.Rank) || string.IsNullOrWhiteSpace(card?.Suit))
                    {
                        continue;
                    }

                    _standardCardRemoved.OnNext(new PlayerHandStandardCardEvent(
                        playerId,
                        new StandardCardData(card.Rank, card.Suit),
                        eventSeq,
                        completedSet,
                        completedSetRank));
                }
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<CardsReceivedDto>("CardsReceived", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                var playerId = payload.PlayerId ?? string.Empty;
                var eventSeq = payload.EventSeq;
                var completedSet = payload.CompletedSet;
                var completedSetRank = payload.CompletedSetRank ?? string.Empty;
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
                            var standardCard = new StandardCardData(card.Rank, card.Suit);
                            standard.Add(standardCard);
                            _standardCardAdded.OnNext(new PlayerHandStandardCardEvent(
                                playerId,
                                standardCard,
                                eventSeq));
                        }
                    }
                }

                _cardsReceived.OnNext(new CardsReceivedEvent(
                    playerId,
                    payload.Source ?? string.Empty,
                    standard,
                    bonus,
                    eventSeq,
                    completedSet,
                    completedSetRank));
            }));
        }

        private void ClearHandlers()
        {
            foreach (var subscription in _handlerSubscriptions)
            {
                subscription?.Dispose();
            }

            _handlerSubscriptions.Clear();
        }

        private sealed class HandCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
        }

        private sealed class PlayerHandUpdatedDto
        {
            public string PlayerId { get; set; }
            public HandCardDto[] Cards { get; set; }
            public long Revision { get; set; }
        }

        private sealed class BonusCardChangedDto
        {
            public string PlayerId { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class CardsReceivedDto
        {
            public string PlayerId { get; set; }
            public string Source { get; set; }
            public TransferCardDto[] Cards { get; set; }
            public long EventSeq { get; set; }
            public bool CompletedSet { get; set; }
            public string CompletedSetRank { get; set; }
        }

        private sealed class CardsTransferredDto
        {
            public string PlayerId { get; set; }
            public string Destination { get; set; }
            public TransferCardDto[] Cards { get; set; }
            public long EventSeq { get; set; }
            public bool CompletedSet { get; set; }
            public string CompletedSetRank { get; set; }
        }

        private sealed class TransferCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
            public string BonusType { get; set; }
        }
    }
}
