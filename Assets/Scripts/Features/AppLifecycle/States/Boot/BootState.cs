using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.States.Home;
using Features.AppLifecycle.Utils;
using Features.WindowSystemImpl.Contents;
using Features.WindowSystemImpl.Data;
using Features.WindowSystemImpl.Templates;
using Chickchilinka.Window;
using Modules.StateMachine.States;
using Modules.StateMachine.Substeps;
using UniState;
using UnityEngine.UI;
using Zenject;

namespace Features.AppLifecycle.States.Boot
{
    [StateBehaviour(ProhibitReturnToState = true)]
    public class BootState : StateWithSubstepsBase
    {
        private readonly ISubstep[] _executionSubsteps;
        private readonly WindowSystem _windowSystem;

        public BootState(
            [Inject(Id = AppLifecycleConst.BootSubstepsId)]
            ISubstep[] executionSubsteps, WindowSystem windowSystem)
        {
            _executionSubsteps = executionSubsteps;
            _windowSystem = windowSystem;
        }

        protected override ISubstep[] GetExecutionSubsteps()
        {
            return _executionSubsteps;
        }

        protected override ISubstep[] GetExitSubsteps()
        {
            return Array.Empty<ISubstep>();
        }

        protected override UniTask<StateTransitionInfo> GetNextStateAsync(CancellationToken token)
        {
            return UniTask.FromResult(Transition.GoTo<HomeState>());
        }

        protected override async UniTask<StateTransitionInfo> OnExecutionSubstepFailure(CancellationToken token, ISubstep substep)
        {
            await _windowSystem.ShowWindowAsync<ErrorWindowContent, ErrorWindowContentData>(
                nameof(BlockerWindowTemplate), new ErrorWindowContentData($"Boot failed at {substep.GetType().Name}",
                    () => { }, true));
            return await base.OnExecutionSubstepFailure(token, substep);
        }
    }
}
