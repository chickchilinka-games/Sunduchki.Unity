using Modules.AppData.Interfaces;
using Modules.AppData.Providers;
using Modules.AppData.Services;
using Zenject;

namespace Modules.AppData.Bootstrap
{
    public class AppDataInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<AppDataService>().AsSingle();
            Container.Bind<IAppDataCache>().To<PlayerPrefsAppDataCache>().AsSingle();
            #if !UNITY_WEBGL
            Container.Bind<IAppDataProvider>().To<FirebaseAppDataProvider>().AsSingle();
            #else 
            Container.Bind<IAppDataProvider>().To<BackendAppDataAdapter>().AsSingle();
            #endif
            Container.BindInterfacesTo<AppDataConsumersCollector>().AsSingle();
            Container.BindInterfacesTo<JsonSerializer>().AsSingle();
        }
    }
}