using System;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using Modules.PlayerHand.Interfaces;
using Modules.PlayerHand.Services;
using R3;
using Zenject;

namespace Modules.PlayerHand.Rules
{
    internal sealed class TrackPlayerHandEventsRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly IPlayerHandEventSource _client;
        private readonly PlayerHandInternalService _internalService;
        private readonly CompositeDisposable _subscriptions = new();
        private bool _keepStateOnReset;

        public TrackPlayerHandEventsRule(
            LobbyService lobbyService,
            IPlayerHandEventSource client,
            PlayerHandInternalService internalService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _internalService = internalService ?? throw new ArgumentNullException(nameof(internalService));
        }

        public void Initialize()
        {
            _client.StandardSnapshot.Subscribe(evt =>
                {
                    _internalService.ApplyStandardSnapshot(evt.PlayerId, evt.Cards);
                })
                .AddTo(_subscriptions);

            _client.StandardCardAdded.Subscribe(evt =>
                {
                    _internalService.AddStandardCard(evt.PlayerId, evt.Card);
                })
                .AddTo(_subscriptions);

            _client.StandardCardRemoved.Subscribe(evt =>
                {
                    _internalService.RemoveStandardCard(evt.PlayerId, evt.Card);
                })
                .AddTo(_subscriptions);

            _client.BonusCardAdded.Subscribe(evt =>
                {
                    _internalService.AddBonusCard(evt.PlayerId, evt.Card);
                })
                .AddTo(_subscriptions);

            _client.BonusCardRemoved.Subscribe(evt =>
                {
                    _internalService.NotifyBonusUsed(evt.PlayerId, evt.Card);
                    _internalService.RemoveBonusCard(evt.PlayerId, evt.Card);
                })
                .AddTo(_subscriptions);

            _client.CardsReceived.Subscribe(evt =>
                {
                    _internalService.NotifyCardsReceived(
                        evt.PlayerId,
                        evt.Source,
                        evt.StandardCards,
                        evt.BonusCards);
                })
                .AddTo(_subscriptions);

            _lobbyService.State.Subscribe(state =>
                {
                    if (state.Status == LobbyStatus.Ended)
                    {
                        _keepStateOnReset = true;
                        return;
                    }

                    if (state.Status != LobbyStatus.Idle || state.Started)
                    {
                        return;
                    }

                    if (_keepStateOnReset)
                    {
                        _keepStateOnReset = false;
                        return;
                    }

                    _internalService.ResetAll();
                })
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _internalService.ResetAll();
        }
    }
}

