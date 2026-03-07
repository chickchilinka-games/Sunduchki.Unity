using System;
using Features.PlayerHandSystemImpl.Presentation.Presenters;
using Modules.DefenseDecisionSystem.Services;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Rules
{
    public sealed class PlayerHandDefensePresentationRule : IInitializable, IDisposable
    {
        private readonly DefenseDecisionService _defenseService;
        private readonly BonusHandInteractionPresenter _bonusPresenter;
        private readonly PlayerHandInteractionPresenter _presenter;
        private readonly CompositeDisposable _subscriptions = new();

        public PlayerHandDefensePresentationRule(
            PlayerHandInteractionPresenter presenter,
            BonusHandInteractionPresenter bonusPresenter,
            DefenseDecisionService defenseService)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _bonusPresenter = bonusPresenter ?? throw new ArgumentNullException(nameof(bonusPresenter));
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
        }

        public void Initialize()
        {
            _defenseService.Prompt
                .Subscribe(prompt =>
                {
                    _presenter.OnDefensePromptChanged(prompt);
                    _bonusPresenter.SetDefensePrompt(prompt);
                })
                .AddTo(_subscriptions);

            _presenter.OnDefensePromptChanged(_defenseService.CurrentPrompt);
            _bonusPresenter.SetDefensePrompt(_defenseService.CurrentPrompt);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
