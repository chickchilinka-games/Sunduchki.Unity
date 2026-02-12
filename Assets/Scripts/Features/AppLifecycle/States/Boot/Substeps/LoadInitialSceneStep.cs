using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.StateMachine.Substeps;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.AppLifecycle.States.Boot.Substeps
{
    public class LoadInitialSceneStep: ISubstep
    {
        public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            await SceneManager.LoadSceneAsync("Initial");
            await UniTask.NextFrame();
            return true;
        }
    }
}
