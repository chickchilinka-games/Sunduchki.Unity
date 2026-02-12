using System;
using Modules.DeckSystem.Services;
using Modules.Lobby.Services;
using R3;
using Zenject;

namespace Modules.DeckSystem.Rules
{
    internal sealed class TrackDeckEventsRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly Interfaces.IDeckEventSource _client;
        private readonly DeckInternalService _internalService;
        private readonly CompositeDisposable _subscriptions = new();

        public TrackDeckEventsRule(
            LobbyService lobbyService,
            Interfaces.IDeckEventSource client,
            DeckInternalService internalService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _internalService = internalService ?? throw new ArgumentNullException(nameof(internalService));
        }

        public void Initialize()
        {
            if (_lobbyService.TryGetDeckConfig(out var deckCount, out var totalCards))
            {
                _internalService.OnDeckConfigured(deckCount, totalCards);
            }

            _lobbyService.GameStarted.Subscribe(payload =>
                {
                    _internalService.OnDeckConfigured(payload.DeckCount, payload.TotalCards);
                })
                .AddTo(_subscriptions);

            _client.Configured.Subscribe(evt =>
                {
                    _internalService.OnDeckConfigured(evt.RemainingCards, evt.TotalCards);
                })
                .AddTo(_subscriptions);

            _client.StandardDrawn.Subscribe(evt =>
                {
                    _internalService.OnStandardCardDrawn(evt.PlayerId, evt.Card);
                })
                .AddTo(_subscriptions);

            _client.BonusDrawn.Subscribe(evt =>
                {
                    _internalService.OnBonusCardDrawn(evt.PlayerId, evt.BonusType);
                })
                .AddTo(_subscriptions);

            _client.Peeked.Subscribe(evt =>
                {
                    _internalService.OnDeckPeeked(evt.PlayerId, evt.Cards);
                })
                .AddTo(_subscriptions);

            _client.Adjusted.Subscribe(evt =>
                {
                    _internalService.OnDeckAdjusted(evt.Delta);
                })
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _internalService.ResetState();
        }
    }
}

