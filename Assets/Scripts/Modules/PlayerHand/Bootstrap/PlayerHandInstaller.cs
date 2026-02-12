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
            Container.Bind<PlayerHandInternalService>().AsSingle();
            Container.Bind<PlayerHandService>().AsSingle();
            Container.BindInterfacesTo<TrackPlayerHandEventsRule>().AsSingle();
            Container.BindInterfacesTo<SignalRPlayerHandClient>().AsSingle();
        }
    }
}
