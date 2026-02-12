using Modules.CardRequestSystem.Model;
using Modules.CardRequestSystem.Rules;
using Modules.CardRequestSystem.Services;
using Zenject;

namespace Modules.CardRequestSystem.Bootstrap
{
    public class CardRequestSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<CardRequestModel>().AsSingle();
            Container.Bind<CardRequestInternalService>().AsSingle();
            Container.Bind<CardRequestService>().AsSingle();
            Container.BindInterfacesTo<TrackCardRequestEventsRule>().AsSingle();
            Container.BindInterfacesTo<SignalRCardRequestClient>().AsSingle();
            Container.BindInterfacesTo<SignalRCardRequestCommandClient>().AsSingle();
        }
    }
}
