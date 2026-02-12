using System;
using Features.PlayerHandSystemImpl.Presenters;
using Modules.TurnSystem.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Rules
{
    public sealed class PlayerHandTurnPresentationRule : IInitializable, IDisposable
    {
        private readonly TurnSequenceService _turnService;
        private readonly BonusHandInteractionPresenter _bonusPresenter;
        private readonly PlayerHandInteractionPresenter _presenter;
        private readonly CompositeDisposable _subscriptions = new();

        public PlayerHandTurnPresentationRule(
            PlayerHandInteractionPresenter presenter,
            BonusHandInteractionPresenter bonusPresenter,
            TurnSequenceService turnService)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _bonusPresenter = bonusPresenter ?? throw new ArgumentNullException(nameof(bonusPresenter));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
        }

        public void Initialize()
        {
            _turnService.State
                .Subscribe(state =>
                {
                    _presenter.OnTurnStateChanged(state.IsLocalTurn);
                    _bonusPresenter.SetAttackTurn(state.IsLocalTurn);
                })
                .AddTo(_subscriptions);

            _presenter.OnTurnStateChanged(_turnService.State.CurrentValue.IsLocalTurn);
            _bonusPresenter.SetAttackTurn(_turnService.State.CurrentValue.IsLocalTurn);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
