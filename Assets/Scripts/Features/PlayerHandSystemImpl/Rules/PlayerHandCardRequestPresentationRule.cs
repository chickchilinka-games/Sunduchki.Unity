using System;
using Features.PlayerHandSystemImpl.Presenters;
using Features.PlayerHandSystemImpl.Storage;
using Modules.CardRequestSystem.Services;
using Modules.PlayerHand.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Rules
{
    public sealed class PlayerHandCardRequestPresentationRule : IInitializable, IDisposable
    {
        private readonly CardRequestService _cardRequestService;
        private readonly PlayerHandService _handService;
        private readonly CardRequestPresentationPresenter _presenter;
        private readonly PlayerHandPresentationContext _context;
        private readonly CompositeDisposable _subscriptions = new();

        public PlayerHandCardRequestPresentationRule(
            CardRequestPresentationPresenter presenter,
            PlayerHandPresentationContext context,
            CardRequestService cardRequestService,
            PlayerHandService handService)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _handService = handService ?? throw new ArgumentNullException(nameof(handService));
        }

        public void Initialize()
        {
            _context.IsActive
                .Subscribe(active =>
                {
                    _presenter.ResetSession();
                    if (active)
                    {
                        _presenter.OnCardRequestStateChanged(_cardRequestService.Current);
                    }
                })
                .AddTo(_subscriptions);

            _cardRequestService.State
                .Subscribe(state => _presenter.OnCardRequestStateChanged(state))
                .AddTo(_subscriptions);

            _handService.CardsReceived
                .Subscribe(evt => _presenter.OnCardsReceivedEvent(evt))
                .AddTo(_subscriptions);

            if (_context.IsActive.CurrentValue)
            {
                _presenter.OnCardRequestStateChanged(_cardRequestService.Current);
            }
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
