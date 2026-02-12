using System;
using Features.PlayerHandSystemImpl.Presenters;
using Features.PlayerHandSystemImpl.Storage;
using Modules.BonusSystem.Config;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Rules
{
    public sealed class BonusHandConfigPresentationRule : IInitializable, IDisposable
    {
        private readonly BonusHandInteractionPresenter _interactionPresenter;
        private readonly BonusCardViewModelStore _store;
        private readonly IBonusCardRulesProvider _rulesProvider;
        private readonly IBonusCardInfoProvider _infoProvider;
        private readonly CompositeDisposable _subscriptions = new();

        public BonusHandConfigPresentationRule(
            BonusHandInteractionPresenter interactionPresenter,
            BonusCardViewModelStore store,
            IBonusCardRulesProvider rulesProvider,
            IBonusCardInfoProvider infoProvider)
        {
            _interactionPresenter = interactionPresenter ?? throw new ArgumentNullException(nameof(interactionPresenter));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
            _infoProvider = infoProvider ?? throw new ArgumentNullException(nameof(infoProvider));
        }

        public void Initialize()
        {
            ApplyConfig();

            _store.Changed
                .Subscribe(_ => ApplyInfo())
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void ApplyConfig()
        {
            var attack = _rulesProvider.GetAttackBonusTypes() ?? Array.Empty<string>();
            var defense = _rulesProvider.GetDefenseBonusTypes() ?? Array.Empty<string>();
            _interactionPresenter.SetBonusRules(attack, defense);
            ApplyInfo();
        }

        private void ApplyInfo()
        {
            foreach (var viewModel in _store.Items)
            {
                if (viewModel == null)
                {
                    continue;
                }

                if (_infoProvider.TryGetInfo(viewModel.BonusCardType, out var info))
                {
                    viewModel.SetInfo(info.Title, info.Description);
                }
                else
                {
                    viewModel.SetInfo(string.Empty, string.Empty);
                }
            }
        }
    }
}
