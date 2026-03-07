using System;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Rules
{
    public sealed class PlayerHandSessionPresentationRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly PlayerHandPresentationContext _context;
        private readonly CompositeDisposable _subscriptions = new();

        public PlayerHandSessionPresentationRule(
            LobbyService lobbyService,
            PlayerHandPresentationContext context)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Initialize()
        {
            _lobbyService.GameStarted
                .Subscribe(_ => Activate())
                .AddTo(_subscriptions);

            _lobbyService.State
                .Subscribe(state =>
                {
                    if (state.Status == LobbyStatus.Ended)
                    {
                        return;
                    }

                    if (state.Status == LobbyStatus.Started || state.Started)
                    {
                        Activate();
                        return;
                    }

                    if (state.Status != LobbyStatus.Started && !state.Started)
                    {
                        _context.Clear();
                    }
                })
                .AddTo(_subscriptions);

            var state = _lobbyService.State.CurrentValue;
            if (state.Status == LobbyStatus.Started || state.Started)
            {
                Activate();
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void Activate()
        {
            var localId = _lobbyService.GetLocalPlayer().Id ?? string.Empty;
            _context.SetLocalPlayerId(localId);
            _context.SetAwaitingAsk(false);
            _context.SetActive(true);
        }
    }
}
