using Modules.Players.Interfaces;
using Modules.Players.Model;
using Modules.Players.Providers;
using Modules.Players.Services;
using Modules.Profiles.Bootstrap;
using Zenject;

namespace Modules.Players.Bootstrap
{
    public class PlayersInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IPlayerProfileProvider>().To<PlayerProfileProvider>().AsSingle();
            Container.Bind<PlayerRosterModel>().AsSingle();
            Container.Bind<PlayerRosterService>().AsSingle();
        }
    }
}
