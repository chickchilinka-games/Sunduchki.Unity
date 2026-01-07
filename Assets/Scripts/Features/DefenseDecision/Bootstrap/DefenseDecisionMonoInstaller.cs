using Modules.DefenseDecisionSystem.Bootstrap;
using Zenject;

namespace Features.DefenseDecision.Bootstrap
{
    public class DefenseDecisionMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<DefenseDecisionSystemInstaller>();
        }
    }
}