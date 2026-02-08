using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AppData.Services;
using Modules.StateMachine.Substeps;

namespace Features.AppLifecycle.States.Boot.Substeps
{
    public class LoadAppDataStep: ISubstep
    {
        private readonly AppDataService _appDataService;

        public LoadAppDataStep(AppDataService appDataService)
        {
            _appDataService = appDataService;
        }

        public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
        { 
            return (await _appDataService.TryLoad()).Success;
        }
    }
}