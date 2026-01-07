using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;

using Modules.StateMachine.Substeps;
using UnityEngine;

namespace Features.AppLifecycle.States.Boot.Substeps
{
    public class InitializeFirebaseSubstep: ISubstep
    {
        public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            await UniTask.Yield(PlayerLoopTiming.Initialization);
            await UniTask.DelayFrame(1, cancellationToken: cancellationToken);
            try
            {
                var status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status == DependencyStatus.Available)
                {
                    var app = FirebaseApp.DefaultInstance;
                    return true;
                }
                Debug.LogError($"[FirebaseInit] Firebase is unavailable: {status}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseInit] Exception: {e}");
                return false;
            }
        }
    }
}