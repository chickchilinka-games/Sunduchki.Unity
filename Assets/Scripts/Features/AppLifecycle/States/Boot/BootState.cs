using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.States.Home;
using Modules.StateMachine.States;
using Modules.StateMachine.Substeps;
using UniState;
using UnityEngine.UI;

namespace Features.AppLifecycle.States.Boot
{
    public class BootState: StateWithSubstepsBase
    {
        protected override ISubstep[] GetExecutionSubsteps()
        {
            return Array.Empty<ISubstep>();
        }

        protected override ISubstep[] GetExitSubsteps()
        {
            return Array.Empty<ISubstep>();
        }

        protected override async UniTask<StateTransitionInfo> GetNextStateAsync(CancellationToken token)
        {
            return Transition.GoTo<HomeState>();
        }
    }
}