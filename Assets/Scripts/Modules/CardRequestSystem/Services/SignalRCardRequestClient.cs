using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using R3;
using UnityEngine;

namespace Modules.CardRequestSystem.Services
{
    internal class SignalRCardRequestClient : ICardRequestEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private readonly Subject<CardRequestedEvent> _requested = new();
        private readonly Subject<CardRequestTransferredEvent> _transferred = new();
        private readonly Subject<CardRequestDeniedEvent> _denied = new();
        private readonly List<IDisposable> _handlerSubscriptions = new();
        private string _localPlayerId = string.Empty;

        public SignalRCardRequestClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public Observable<CardRequestedEvent> Requested => _requested;
        public Observable<CardRequestTransferredEvent> Transferred => _transferred;
        public Observable<CardRequestDeniedEvent> Denied => _denied;

        public UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[CardRequestSystem] Cannot track requests: missing game or player id.");
                return UniTask.CompletedTask;
            }

            _localPlayerId = session.PlayerId ?? string.Empty;
            ClearHandlers();
            RegisterHandlers();
            return UniTask.CompletedTask;
        }

        public UniTask DisconnectAsync()
        {
            ClearHandlers();
            _localPlayerId = string.Empty;
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            ClearHandlers();
        }

        private void RegisterHandlers()
        {
            ClearHandlers();
            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<CardsRequestedDto>("CardsRequested", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _requested.OnNext(new CardRequestedEvent(
                    payload.FromPlayerId,
                    payload.TargetPlayerId,
                    payload.Rank));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<HandDeltaDto>("HandDelta", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                if (string.Equals(payload.Destination, "chest", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(payload.Source, "deck", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var sourcePlayerId = payload.Source ?? string.Empty;
                var destinationPlayerId = payload.Destination ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sourcePlayerId) || string.IsNullOrWhiteSpace(destinationPlayerId))
                {
                    return;
                }

                var localPlayerId = _localPlayerId;
                if (!string.IsNullOrWhiteSpace(localPlayerId) &&
                    !string.Equals(sourcePlayerId, localPlayerId, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(destinationPlayerId, localPlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var mapped = new List<CardTransferCardData>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var cards = ResolveTransferCards(payload);
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
                    payload.ActionId ?? string.Empty,
                    sourcePlayerId,
                    destinationPlayerId,
                    mapped));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<NoCardsResponseDto>("NoCardsResponse", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _denied.OnNext(new CardRequestDeniedEvent(
                    payload.FromPlayerId,
                    payload.TargetPlayerId,
                    payload.Rank));
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

        private sealed class CardsRequestedDto
        {
            public string FromPlayerId { get; set; }
            public string TargetPlayerId { get; set; }
            public string Rank { get; set; }
        }

        private static IReadOnlyList<TransferCardDto> ResolveTransferCards(HandDeltaDto payload)
        {
            if (payload == null)
            {
                return Array.Empty<TransferCardDto>();
            }

            var removed = payload.RemovedCards;
            if (removed != null && removed.Length > 0)
            {
                return removed;
            }

            var added = payload.AddedCards;
            if (added != null && added.Length > 0)
            {
                return added;
            }

            return Array.Empty<TransferCardDto>();
        }

        private sealed class HandDeltaDto
        {
            public string ActionId { get; set; }
            public string PlayerId { get; set; }
            public string Source { get; set; }
            public string Destination { get; set; }
            public TransferCardDto[] AddedCards { get; set; }
            public TransferCardDto[] RemovedCards { get; set; }
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
