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
        private readonly Subject<PlayerHandSnapshotEvent> _handSnapshot = new();
        private readonly Subject<PlayerHandDeltaEvent> _handDelta = new();
        private readonly List<IDisposable> _handlerSubscriptions = new();

        public SignalRPlayerHandClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public Observable<PlayerHandSnapshotEvent> HandSnapshot => _handSnapshot;
        public Observable<PlayerHandDeltaEvent> HandDelta => _handDelta;

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

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<HandSnapshotDto>("HandSnapshot", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                var standardCards = new List<StandardCardData>();
                var bonusCards = new List<BonusCardData>();
                MapCards(payload.Cards, standardCards, bonusCards);

                _handSnapshot.OnNext(new PlayerHandSnapshotEvent(
                    payload.PlayerId ?? string.Empty,
                    standardCards,
                    bonusCards,
                    payload.Revision));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<HandDeltaDto>("HandDelta", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                var addedStandardCards = new List<StandardCardData>();
                var addedBonusCards = new List<BonusCardData>();
                var removedStandardCards = new List<StandardCardData>();
                var removedBonusCards = new List<BonusCardData>();
                MapCards(payload.AddedCards, addedStandardCards, addedBonusCards);
                MapCards(payload.RemovedCards, removedStandardCards, removedBonusCards);

                _handDelta.OnNext(new PlayerHandDeltaEvent(
                    payload.ActionId ?? string.Empty,
                    payload.PlayerId ?? string.Empty,
                    payload.Source ?? string.Empty,
                    payload.Destination ?? string.Empty,
                    addedStandardCards,
                    addedBonusCards,
                    removedStandardCards,
                    removedBonusCards,
                    payload.EventSeq,
                    payload.CompletedSet,
                    payload.CompletedSetRank ?? string.Empty));
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

        private static void MapCards(
            IReadOnlyList<TransferCardDto> cards,
            ICollection<StandardCardData> standardCards,
            ICollection<BonusCardData> bonusCards)
        {
            if (cards == null)
            {
                return;
            }

            foreach (var card in cards)
            {
                if (!string.IsNullOrWhiteSpace(card?.BonusType))
                {
                    bonusCards.Add(new BonusCardData(card.BonusType));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(card?.Rank) && !string.IsNullOrWhiteSpace(card?.Suit))
                {
                    standardCards.Add(new StandardCardData(card.Rank, card.Suit));
                }
            }
        }

        private sealed class HandSnapshotDto
        {
            public string PlayerId { get; set; }
            public TransferCardDto[] Cards { get; set; }
            public long Revision { get; set; }
        }

        private sealed class HandDeltaDto
        {
            public string ActionId { get; set; }
            public string PlayerId { get; set; }
            public string Source { get; set; }
            public string Destination { get; set; }
            public TransferCardDto[] AddedCards { get; set; }
            public TransferCardDto[] RemovedCards { get; set; }
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
