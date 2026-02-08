using System;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Modules.AuthenticationSystem.Providers
{
    public sealed class TelegramCredentialProvider : ICredentialProvider
    {
        public AuthType AuthType => AuthType.Telegram;

        public UniTask<AuthCredential> AcquireCredentialAsync()
        {
            var initData = TelegramWebAppBridge.GetInitData();
            if (string.IsNullOrWhiteSpace(initData))
            {
                throw new InvalidOperationException("Telegram initData is not available.");
            }

            var credential = AuthCredentialBuilder
                .For(AuthType)
                .WithParameter(AuthCredentialBuilder.ParameterKeys.InitData, initData)
                .Build();

            return UniTask.FromResult(credential);
        }
    }
}
