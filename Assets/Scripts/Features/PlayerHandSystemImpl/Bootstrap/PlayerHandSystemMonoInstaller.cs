using System;
using Features.PlayerHandSystemImpl.Commands;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Interfaces;
using Features.PlayerHandSystemImpl.Presentation.Presenters;
using Features.PlayerHandSystemImpl.Presentation.Rules;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Features.PlayerHandSystemImpl.Providers;
using Features.PlayerHandSystemImpl.Utils;
using Features.PlayerHandSystemImpl.View;
using Modules.PlayerHand.Bootstrap;
using UnityEngine;
using Zenject;

namespace Features.PlayerHandSystemImpl.Bootstrap
{
    public class PlayerHandSystemMonoInstaller : MonoInstaller
    {
        public const string StandardCardViewPrefabBindingId = "player_hand.standard_prefab";

        [SerializeField] private RankStackView _standardCardViewPrefab;
        [SerializeField] private BonusCardView _bonusCardViewPrefab;

        public override void InstallBindings()
        {
            Container.Install<PlayerHandInstaller>();
            Container.BindInterfacesAndSelfTo<PlayerHandPresentationContext>().AsSingle();
            Container.BindInterfacesAndSelfTo<RankStackViewModelStorage>().AsSingle();
            Container.BindInterfacesAndSelfTo<BonusCardViewModelStorage>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerHandPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerHandInteractionPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<BonusHandPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<BonusHandInteractionPresenter>().AsSingle();
            Container.BindInterfacesAndSelfTo<CardRequestPresentationPresenter>().AsSingle();
            Container.BindInterfacesTo<PlayerHandCommands>().AsSingle();
            Container.Bind<IRankStackViewModelFactory>().To<RankStackViewModelFactory>().AsSingle();
            Container.BindInterfacesTo<PlayerHandPresentationCoordinatorRule>().AsSingle();
            Container.BindInterfacesTo<BonusHandConfigPresentationRule>().AsSingle();
            Container.BindInterfacesTo<BonusHandUsePresentationRule>().AsSingle();
            Container.Bind<ITargetPlayerSelector>().To<SingleOpponentSelector>().AsSingle();
            Container.Bind<CardSpriteResolver>().AsSingle();

            BindStandardPool();
            BindBonusPool();
        }

        private void BindStandardPool()
        {
            if (_standardCardViewPrefab == null)
            {
                throw new InvalidOperationException("Standard card view prefab is not assigned.");
            }

            Container.BindInstance(_standardCardViewPrefab).WithId(StandardCardViewPrefabBindingId);

            Container.BindMemoryPool<RankStackView, RankStackViewPool>()
                .WithInitialSize(0)
                .FromComponentInNewPrefab(_standardCardViewPrefab)
                .UnderTransformGroup("PlayerHand_StandardPool");
        }

        private void BindBonusPool()
        {
            if (_bonusCardViewPrefab == null)
            {
                throw new InvalidOperationException("Bonus card view prefab is not assigned.");
            }

            Container.BindMemoryPool<BonusCardView, BonusCardViewPool>()
                .WithInitialSize(0)
                .FromComponentInNewPrefab(_bonusCardViewPrefab)
                .UnderTransformGroup("PlayerHand_BonusPool");
        }
    }
}
