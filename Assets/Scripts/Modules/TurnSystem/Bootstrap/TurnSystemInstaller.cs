using Modules.TurnSystem.Model;
using Modules.TurnSystem.Rules;
using Modules.TurnSystem.Services;
using Zenject;

namespace Modules.TurnSystem.Bootstrap
{
    public class TurnSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<TurnSequenceModel>().AsSingle();
            Container.Bind<TurnSequenceInternalService>().AsSingle();
            Container.Bind<TurnSequenceService>().AsSingle();
            Container.BindInterfacesTo<TrackTurnEventsRule>().AsSingle();
            Container.BindInterfacesTo<SignalRTurnClient>().AsSingle();
        }
    }
}
