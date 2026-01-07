using Modules.BonusSystem.Bootstrap;
using NUnit.Framework;
using Zenject;

namespace Features.BonusSystemImpl.Bootstrap
{
    public class BonusSystemMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<BonusSystemInstaller>();
        }
    }
}