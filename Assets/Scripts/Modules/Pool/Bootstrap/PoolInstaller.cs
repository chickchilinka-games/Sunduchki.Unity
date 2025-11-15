using Base.Pool.Services;
using Zenject;

namespace Base.Pool.Bootstrap
{
    public class PoolInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<GeneralPool>().AsSingle();
        }
    }
}
