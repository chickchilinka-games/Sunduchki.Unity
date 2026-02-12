using System;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using R3;
using Zenject;

namespace Modules.DefenseDecisionSystem.Rules
{
    internal sealed class TrackDefenseDecisionRequestsRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly IDefenseDecisionEventSource _client;
        private readonly DefenseDecisionService _service;
        private readonly CompositeDisposable _subscriptions = new();

        public TrackDefenseDecisionRequestsRule(
            LobbyService lobbyService,
            IDefenseDecisionEventSource client,
            DefenseDecisionService service)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public void Initialize()
        {
            _client.Requested.Subscribe(evt =>
                {
                    var prompt = new DefenseDecisionPrompt(
                        evt.AskerId,
                        evt.TargetId,
                        evt.Rank,
                        evt.DefenseOptions);
                    _service.PublishPrompt(prompt);
                })
                .AddTo(_subscriptions);

            _lobbyService.State.Subscribe(state =>
                {
                    if (state.Status != LobbyStatus.Idle || state.Started)
                    {
                        return;
                    }

                    _service.ClearPrompt();
                })
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _service.ClearPrompt();
        }
    }
}

