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
            Container.Bind<IAppDataProvider>().To<FirebaseAppDataProvider>().AsSingle();
            Container.BindInterfacesTo<AppDataConsumersCollector>().AsSingle();
            Container.BindInterfacesTo<JsonSerializer>().AsSingle();
        }
    }
}