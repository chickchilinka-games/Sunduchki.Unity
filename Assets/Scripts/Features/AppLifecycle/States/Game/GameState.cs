using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.States.Game.Substates;
using Modules.StateMachine.States;
using UniState;
using UnityEngine.SceneManagement;

namespace Features.AppLifecycle.States.Game
{
    public class GameState: StateBase
    {
        private readonly ISubStateMachine _subStateMachine;

        public GameState(ISubStateMachine subStateMachine)
        {
            _subStateMachine = subStateMachine;
        }

        public override async UniTask<StateTransitionInfo> Execute(CancellationToken token)
        {
            await SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single).ToUniTask(cancellationToken: token);
            await _subStateMachine.Execute<WaitTurnGameSubstate>(token);
            return Transition.GoBack();
        }

        public override async UniTask Exit(CancellationToken token)
        {
            
        }
    }
}