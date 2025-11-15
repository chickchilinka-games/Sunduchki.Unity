using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.StateMachine.Substeps;
using UniState;

namespace Modules.StateMachine.States
{
    public abstract class StateWithSubstepsBase : StateWithSubstepsBase<EmptyPayload>
    {
        
    }
    
    public abstract class StateWithSubstepsBase<TPayload> : StateBase<TPayload>
    {
        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            foreach (var substep in GetExecutionSubsteps())
            {
                var success = await substep.ExecuteAsync(token);
                if (!success)
                {
                    return await OnExecutionSubstepFailure(token);
                }
            }
            
            return await GetNextStateAsync(token);
        }

        public override async UniTask Exit(CancellationToken token)
        {
            foreach (var substep in GetExitSubsteps())
            {
                var success = await substep.ExecuteAsync(token);
                if (!success)
                {
                    await OnExitSubstepFailure(token);
                }
            }
        }

        protected abstract ISubstep[] GetExecutionSubsteps();
        
        protected abstract ISubstep[] GetExitSubsteps();
        
        protected abstract UniTask<StateTransitionInfo> GetNextStateAsync(CancellationToken token);
        
        protected virtual UniTask<StateTransitionInfo> OnExecutionSubstepFailure(CancellationToken token)
        {
            return UniTask.FromResult(Transition.GoToExit());
        }
        
        protected virtual UniTask OnExitSubstepFailure(CancellationToken token)
        {
            throw new InvalidOperationException("Sub-step execution on state exit failed.");
        }
    }
}