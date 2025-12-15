using System;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Rules;
using Features.PlayerHandSystemImpl.Service;
using Features.PlayerHandSystemImpl.View;
using Modules.PlayerHand.Bootstrap;
using UnityEngine;
using Zenject;

namespace Features.PlayerHandSystemImpl.Bootstrap
{
    public class PlayerHandSystemMonoInstaller : MonoInstaller
    {
        [SerializeField] private StandardCardView _standardCardViewPrefab;
        [SerializeField] private BonusCardView _bonusCardViewPrefab;

        public override void InstallBindings()
        {
            Container.Install<PlayerHandInstaller>();
            Container.Bind<PlayerHandViewService>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerHandViewRule>().AsSingle();

            BindStandardPool();
            BindBonusPool();
        }

        private void BindStandardPool()
        {
            if (_standardCardViewPrefab == null)
            {
                throw new InvalidOperationException("Standard card view prefab is not assigned.");
            }

            Container.BindMemoryPool<StandardCardView, StandardCardViewPool>()
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
