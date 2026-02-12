using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AuthenticationSystemImpl.Views;
using Features.LoadingScreen.Services;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Exceptions;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Features.AuthenticationSystemImpl.Providers
{
    public class EditorEmailCredentialProvider : ICredentialProvider
    {
        private readonly WindowSystem _windowSystem;
        private readonly LoadingScreenService _loadingScreenService;

        public EditorEmailCredentialProvider(WindowSystem windowSystem, LoadingScreenService loadingScreenService)
        {
            _windowSystem = windowSystem;
            _loadingScreenService = loadingScreenService;
        }

        public AuthType AuthType => AuthType.Email;

        public async UniTask<AuthCredential> AcquireCredentialAsync()
        {
            try
            {
                var ucs = new UniTaskCompletionSource<(string email, string password)>();
                var windowData = new EmailPasswordInputContent.InputData()
                {
                    OnComplete = ucs
                };
                _loadingScreenService.Hide();

                await _windowSystem.ShowWindowAsync<EmailPasswordInputContent, EmailPasswordInputContent.InputData>(
                    nameof(BlockerWindowTemplate), windowData);

                var result = await ucs.Task;

                _loadingScreenService.Show();

                return AuthCredentialBuilder.For(AuthType.Email)
                    .WithUsername(result.email)
                    .WithPassword(result.password)
                    .Build();
            }
            catch (OperationCanceledException)
            {
                throw new UserCancelledException("User cancelled email authentication");
            }
            finally
            {
                await _windowSystem.CloseWindowsWithContentAsync<EmailPasswordInputContent>();
            }
        }
    }
}
