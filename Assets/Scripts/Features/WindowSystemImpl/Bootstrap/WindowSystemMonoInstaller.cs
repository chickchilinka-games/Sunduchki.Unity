using Chickchilinka.Window;
using Chickchilinka.Window.Installers;
using Zenject;

namespace Features.WindowSystemImpl.Bootstrap
{
    public class WindowSystemMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<WindowSystemInstaller>();
        }
    }
}
