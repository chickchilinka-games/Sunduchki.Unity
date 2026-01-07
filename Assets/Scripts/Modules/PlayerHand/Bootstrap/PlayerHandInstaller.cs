using Modules.PlayerHand.Interfaces;
using Modules.PlayerHand.Model;
using Modules.PlayerHand.Rules;
using Modules.PlayerHand.Services;
using Zenject;

namespace Modules.PlayerHand.Bootstrap
{
    public class PlayerHandInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<PlayerHandModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerHandService>().AsSingle();
            Container.BindInterfacesTo<PlayerHandSignalRelay>().AsSingle();
            Container.Bind<IPlayerHandSignalClient>().To<SignalRPlayerHandClient>().AsSingle();
            Container.BindInterfacesTo<TrackPlayerHandOnLobbyConnectRule>().AsSingle();
        }
    }
}
