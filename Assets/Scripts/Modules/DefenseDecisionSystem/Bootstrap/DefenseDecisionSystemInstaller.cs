using Modules.DefenseDecisionSystem.Rules;
using Modules.DefenseDecisionSystem.Services;
using Zenject;

namespace Modules.DefenseDecisionSystem.Bootstrap
{
    public sealed class DefenseDecisionSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<SignalRDefenseDecisionClient>().AsSingle();
            Container.Bind<DefenseDecisionService>().AsSingle();
            Container.BindInterfacesTo<TrackDefenseDecisionRequestsRule>().AsSingle();
        }
    }
}
