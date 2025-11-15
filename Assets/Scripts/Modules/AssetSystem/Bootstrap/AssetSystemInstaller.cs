using Modules.AppData.Interfaces;
using Modules.AssetSystem.Observables;
using Modules.AssetSystem.Providers;
using Modules.AssetSystem.Services;
using Modules.AssetSystem.Storage;
using Zenject;

namespace Modules.AssetSystem.Bootstrap
{
    public class AssetSystemMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<AssetService>().AsSingle();
            Container.Bind<AssetCache>().AsSingle()
                .WhenInjectedInto<AssetService>();
            Container.BindInterfacesTo<ResourcesAssetProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<AssetUnloadService>().AsSingle();
            Container.Bind(typeof(IAppStateListener), typeof(AssetUnloadObservable))
                .To<StageSwitchObservable>().AsCached();
        }
    }
}