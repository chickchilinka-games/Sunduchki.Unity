using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Modules.AuthenticationSystem.Providers
{
    public class WebAppleCredentialProvider: ICredentialProvider
    {
        public AuthType AuthType => AuthType.Apple;
        public UniTask<AuthCredential> AcquireCredentialAsync()
        {
            return UniTask.FromResult(AuthCredentialBuilder.For(AuthType).Build());
        }
    }
}