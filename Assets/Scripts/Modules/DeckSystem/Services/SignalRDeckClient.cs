using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DeckSystem.Data;
using Modules.DeckSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using R3;
using UnityEngine;

namespace Modules.DeckSystem.Services
{
    internal class SignalRDeckClient : IDeckEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private readonly Subject<DeckConfiguredEvent> _configured = new();
        private readonly Subject<DeckCardDrawnEvent> _standardDrawn = new();
        private readonly Subject<DeckBonusCardDrawnEvent> _bonusDrawn = new();
        private readonly Subject<DeckPeekedEvent> _peeked = new();
        private readonly Subject<DeckAdjustedEvent> _adjusted = new();
        private readonly List<IDisposable> _handlerSubscriptions = new();

        public SignalRDeckClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public Observable<DeckConfiguredEvent> Configured => _configured;
        public Observable<DeckCardDrawnEvent> StandardDrawn => _standardDrawn;
        public Observable<DeckBonusCardDrawnEvent> BonusDrawn => _bonusDrawn;
        public Observable<DeckPeekedEvent> Peeked => _peeked;
        public Observable<DeckAdjustedEvent> Adjusted => _adjusted;

        public UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[DeckSystem] Cannot start tracking deck: missing game or player id.");
                return UniTask.CompletedTask;
            }

            ClearHandlers();
            RegisterHandlers();
            Debug.Log($"[DeckSystem] Subscribed to deck updates for {session.PlayerId}.");
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
            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<DeckConfiguredDto>("DeckConfigured", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _configured.OnNext(new DeckConfiguredEvent(payload.RemainingCards, payload.TotalCards));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<BonusCardDrawnDto>("DrewBonusFromDeck", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _bonusDrawn.OnNext(new DeckBonusCardDrawnEvent(payload.PlayerId, payload.BonusType));
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<PeekedAtDeckDto>("PeekedAtDeck", payload =>
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
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<PeekedAtDecksDto>("PeekedAtDecks", payload =>
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
            }));

            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<DeckAdjustedDto>("DeckAdjusted", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _adjusted.OnNext(new DeckAdjustedEvent(payload.Delta));
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

        private sealed class DeckConfiguredDto
        {
            public int RemainingCards { get; set; }
            public int TotalCards { get; set; }
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

        private sealed class PeekedCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class PeekedAtDecksDto
        {
            public string PlayerId { get; set; }
            public PeekedCardDto[] Cards { get; set; }
        }

        private sealed class DeckAdjustedDto
        {
            public int Delta { get; set; }
        }
    }
}
