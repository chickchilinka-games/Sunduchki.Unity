using Features.AppLifecycle.Services;
using Features.AppLifecycle.States.Boot;
using Features.AppLifecycle.States.Home;
using Features.AppLifecycle.Utils;
using Modules.StateMachine.States;
using Modules.StateMachine.Substeps;
using UniState;
using Zenject;

namespace Features.AppLifecycle.Bootstrap
{
    public class AppLifecycleInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<AppLifecycleService>().AsSingle();
            Container.BindStateMachineAsSingle<IAppStateMachine, AppStateMachine>();
            Container.BindStateMachine<ISubStateMachine, SubStateMachine>();

            Container.BindStateAsSingle<BootState>();
            Container.BindStateAsSingle<HomeState>();
            InstallBootSubsteps();
            InstallHomeSubStates();
        }

        private void InstallBootSubsteps()
        {

        }
        
        private void InstallHomeSubStates()
        {
            
        }

        private void InstallBootStep<TStep>() where TStep : ISubstep
        {
            Container.Bind<ISubstep>()
                .WithId(AppLifecycleConst.BootSubstepsId)
                .To<TStep>()
                .AsTransient();
        }
    }
}
