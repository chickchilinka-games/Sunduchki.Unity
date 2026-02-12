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
            Container.Bind<DeckService>().AsSingle();
            Container.Bind<DeckInternalService>().AsSingle();
            Container.BindInterfacesTo<SignalRDeckClient>().AsSingle();
            Container.BindInterfacesTo<TrackDeckEventsRule>().AsSingle();
        }
    }
}
