using Modules.Players.Interfaces;
using Modules.Players.Model;
using Modules.Players.Services;
using Zenject;

namespace Modules.Players.Bootstrap
{
    public class PlayersInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<PlayerRosterModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerRosterService>().AsSingle();
        }
    }
}
