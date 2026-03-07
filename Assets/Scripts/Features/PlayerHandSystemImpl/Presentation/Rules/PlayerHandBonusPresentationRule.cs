using System;
using Features.PlayerHandSystemImpl.Presentation.Presenters;
using Modules.BonusSystem.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Rules
{
    public sealed class PlayerHandBonusPresentationRule : IInitializable, IDisposable
    {
        private readonly BonusActionService _bonusService;
        private readonly BonusHandInteractionPresenter _interactionPresenter;
        private readonly CompositeDisposable _subscriptions = new();

        public PlayerHandBonusPresentationRule(
            BonusHandInteractionPresenter interactionPresenter,
            BonusActionService bonusService)
        {
            _interactionPresenter = interactionPresenter ?? throw new ArgumentNullException(nameof(interactionPresenter));
            _bonusService = bonusService ?? throw new ArgumentNullException(nameof(bonusService));
        }

        public void Initialize()
        {
            _bonusService.IsExecuting
                .Subscribe(isExecuting => _interactionPresenter.SetBonusExecuting(isExecuting))
                .AddTo(_subscriptions);

            _interactionPresenter.SetBonusExecuting(_bonusService.IsExecuting.CurrentValue);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
