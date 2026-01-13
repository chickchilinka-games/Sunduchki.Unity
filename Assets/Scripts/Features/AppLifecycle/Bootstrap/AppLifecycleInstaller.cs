using Features.AppLifecycle.Services;
using Features.AppLifecycle.States.Boot;
using Features.AppLifecycle.States.Boot.Substeps;
using Features.AppLifecycle.States.Game;
using Features.AppLifecycle.States.Game.Substates;
using Features.AppLifecycle.States.Home;
using Features.AppLifecycle.States.Lobby;
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
            Container.Bind<LobbyFlowService>().AsSingle();
            Container.Bind<GameResultsFlowService>().AsSingle();
            Container.Bind<GameMenuFlowService>().AsSingle();
            Container.BindStateMachineAsSingle<IAppStateMachine, AppStateMachine>();
            Container.BindStateMachine<ISubStateMachine, SubStateMachine>();

            Container.BindStateAsSingle<BootState>();
            Container.BindStateAsSingle<HomeState>();
            Container.BindStateAsSingle<LobbyState>();
            Container.BindStateAsSingle<GameState>();
            
            InstallGameSubStates();
            InstallBootSubsteps();
            InstallHomeSubStates();
        }

        private void InstallBootSubsteps()
        {
            InstallBootStep<LoadInitialSceneStep>();
            InstallBootStep<InitializeFirebaseSubstep>();
            InstallBootStep<LoadAppDataStep>();
            InstallBootStep<AuthorizationSubstep>();
        }

        private void InstallGameSubStates()
        {
            Container.BindStateAsSingle<OpponentDefenseGameSubstate>();
            Container.BindStateAsSingle<OpponentTurnGameSubstate>();
            Container.BindStateAsSingle<PlayerDefenseGameSubstate>();
            Container.BindStateAsSingle<PlayerTurnGameSubstate>();
            Container.BindStateAsSingle<ResultsGameSubstate>();
            Container.BindStateAsSingle<WaitTurnGameSubstate>();
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
