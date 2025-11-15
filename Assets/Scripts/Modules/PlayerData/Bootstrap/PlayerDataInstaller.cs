using Modules.PlayerData.Interfaces;
using Modules.PlayerData.Providers;
using Modules.PlayerData.Services;
using Zenject;

namespace Modules.PlayerData.Bootstrap
{
    public class PlayerDataInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<PlayerDataService>().AsSingle();
            Container.Bind<IPlayerDataCache>().To<PlayerPrefsPlayerDataCache>().AsSingle();
            Container.BindInterfacesTo<JsonSerializer>().AsSingle();
            Container.BindInterfacesTo<PlayerDataConsumersCollector>().AsSingle();
        }
    }
}