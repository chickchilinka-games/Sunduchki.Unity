using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AuthenticationSystemImpl.Views;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Services;
using Modules.StateMachine.Substeps;
using R3;
using UnityEngine;

namespace Features.AppLifecycle.States.Boot.Substeps
{
    public class AuthorizationSubstep : ISubstep
    {
        private readonly AuthenticationService _authenticationService;
        private readonly WindowSystem _windowSystem;

        public AuthorizationSubstep(AuthenticationService authenticationService, WindowSystem windowSystem)
        {
            _authenticationService = authenticationService;
            _windowSystem = windowSystem;
        }

        public async UniTask<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            var autoSignInResult = await _authenticationService.TryAutoSignInAsync();
            if (autoSignInResult.Success)
                return true;

            await _windowSystem.ShowWindowAsync<AuthenticationWindowContent>(nameof(BlockerWindowTemplate));
            await _authenticationService.AuthStatusStream.Where(status => status == AuthStatus.SignedIn)
                .FirstAsync(cancellationToken: cancellationToken);
            await _windowSystem.CloseWindowsWithContentAsync<AuthenticationWindowContent>();
            return true;
        }
    }
}
