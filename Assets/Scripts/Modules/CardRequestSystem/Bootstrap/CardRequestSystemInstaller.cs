using Modules.CardRequestSystem.Interfaces;
using Modules.CardRequestSystem.Model;
using Modules.CardRequestSystem.Services;
using Zenject;

namespace Modules.CardRequestSystem.Bootstrap
{
    public class CardRequestSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<CardRequestModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<CardRequestService>().AsSingle();
            Container.Bind<CardRequestCommandService>().AsSingle();
            Container.Bind<CardRequestTrackingService>().AsSingle();
            Container.BindInterfacesTo<CardRequestSignalRelay>().AsSingle();
            Container.Bind<ICardRequestSignalClient>().To<SignalRCardRequestClient>().AsSingle();
            Container.Bind<ICardRequestCommandClient>().To<SignalRCardRequestCommandClient>().AsSingle();
        }
    }
}
