using Modules.DeckSystem.Bootstrap;
using Zenject;

namespace Features.DeckSystemImpl.Bootstrap
{
    public class DeckSystemMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<DeckSystemInstaller>();
        }
    }
}