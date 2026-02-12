using Modules.CardRequestSystem.Bootstrap;
using Zenject;

namespace Features.CardRequestSystemImpl.Bootstrap
{
    public class CardRequestSystemMonoInstaller:MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<CardRequestSystemInstaller>();
        }
    }
}
