using System;
using Features.PlayerHandSystemImpl.Presenters;
using Modules.Lobby.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Rules
{
    public sealed class PlayerHandSetCompletionPresentationRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly PlayerHandSetCompletionPresenter _presenter;
        private readonly CompositeDisposable _subscriptions = new();

        public PlayerHandSetCompletionPresentationRule(
            LobbyService lobbyService,
            PlayerHandSetCompletionPresenter presenter)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        public void Initialize()
        {
            _lobbyService.ChestUpdated
                .Subscribe(payload => _presenter.OnChestUpdated(payload))
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
