using System;
using System.Linq;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using Modules.TurnSystem.Interfaces;
using Modules.TurnSystem.Services;
using R3;
using Zenject;

namespace Modules.TurnSystem.Rules
{
    internal sealed class TrackTurnEventsRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly ITurnEventSource _client;
        private readonly TurnSequenceInternalService _internalService;
        private readonly CompositeDisposable _subscriptions = new();

        public TrackTurnEventsRule(
            LobbyService lobbyService,
            ITurnEventSource client,
            TurnSequenceInternalService internalService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _internalService = internalService ?? throw new ArgumentNullException(nameof(internalService));
        }

        public void Initialize()
        {
            if (_lobbyService.TryGetLocalPlayerId(out var localId))
            {
                _internalService.SetLocalPlayer(localId);
            }

            _lobbyService.Players.Subscribe(players =>
                {
                    var local = players?.FirstOrDefault(player => player.IsLocal);
                    if (!string.IsNullOrWhiteSpace(local?.Id))
                    {
                        _internalService.SetLocalPlayer(local.Value.Id);
                    }
                })
                .AddTo(_subscriptions);

            _client.TurnAdvanced.Subscribe(playerId =>
                {
                    _internalService.AdvanceTo(playerId);
                })
                .AddTo(_subscriptions);

            _lobbyService.State.Subscribe(state =>
                {
                    if (state.Status != LobbyStatus.Idle || state.Started)
                    {
                        return;
                    }

                    _internalService.Reset();
                })
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _internalService.Reset();
        }
    }
}

