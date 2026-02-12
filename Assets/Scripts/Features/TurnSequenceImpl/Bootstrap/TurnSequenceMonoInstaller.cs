using Modules.TurnSystem.Bootstrap;
using Zenject;

namespace Features.TurnSequenceImpl.Bootstrap
{
    public class TurnSequenceMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<TurnSystemInstaller>();
        }
    }
}
