using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Modules.AuthenticationSystem.Providers
{
    public sealed class WebGoogleCredentialProvider : ICredentialProvider
    {
        public AuthType AuthType => AuthType.Google;

        public UniTask<AuthCredential> AcquireCredentialAsync()
        {
            var scopes = new[] { "email", "profile" };

            var credential = AuthCredentialBuilder
                .For(AuthType)
                .WithParameter("scopes", scopes)
                .Build();

            return UniTask.FromResult(credential);
        }
    }
}
