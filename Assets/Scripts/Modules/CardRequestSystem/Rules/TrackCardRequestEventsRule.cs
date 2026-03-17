using System;
using Modules.CardRequestSystem.Interfaces;
using Modules.CardRequestSystem.Services;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using R3;
using Zenject;

namespace Modules.CardRequestSystem.Rules
{
    internal sealed class TrackCardRequestEventsRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly ICardRequestEventSource _client;
        private readonly CardRequestInternalService _internalService;
        private readonly CompositeDisposable _subscriptions = new();

        public TrackCardRequestEventsRule(
            LobbyService lobbyService,
            ICardRequestEventSource client,
            CardRequestInternalService internalService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _internalService = internalService ?? throw new ArgumentNullException(nameof(internalService));
        }

        public void Initialize()
        {
            _client.Requested.Subscribe(evt =>
                {
                    _internalService.OnCardsRequested(evt.FromPlayerId, evt.TargetPlayerId, evt.Rank);
                })
                .AddTo(_subscriptions);

            _client.Transferred.Subscribe(evt =>
                {
                    _internalService.OnCardsTransferred(evt.ActionId, evt.FromPlayerId, evt.TargetPlayerId, evt.Cards);
                })
                .AddTo(_subscriptions);

            _client.Denied.Subscribe(evt =>
                {
                    _internalService.OnNoCardsResponse(evt.FromPlayerId, evt.TargetPlayerId, evt.Rank);
                })
                .AddTo(_subscriptions);

            _lobbyService.State.Subscribe(state =>
                {
                    if (state.Status != LobbyStatus.Idle || state.Started)
                    {
                        return;
                    }

                    _internalService.ResetState();
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

