using Features.BonusSystemImpl.Presenters;
using Modules.BonusSystem.Bootstrap;
using Zenject;

namespace Features.BonusSystemImpl.Bootstrap
{
    public class BonusSystemMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<BonusSystemInstaller>();
            Container.BindInterfacesAndSelfTo<OpponentUsedBonusPresenter>().AsSingle();
        }
    }
}
