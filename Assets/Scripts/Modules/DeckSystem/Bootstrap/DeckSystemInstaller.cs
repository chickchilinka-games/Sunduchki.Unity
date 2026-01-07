using Modules.DeckSystem.Interfaces;
using Modules.DeckSystem.Model;
using Modules.DeckSystem.Rules;
using Modules.DeckSystem.Services;
using Zenject;

namespace Modules.DeckSystem.Bootstrap
{
    public class DeckSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<DeckModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<DeckService>().AsSingle();
            Container.BindInterfacesTo<DeckSignalRelay>().AsSingle();
            Container.Bind<IDeckSignalClient>().To<SignalRDeckClient>().AsSingle();
            Container.BindInterfacesTo<TrackDeckOnLobbyConnectRule>().AsSingle();
        }
    }
}
