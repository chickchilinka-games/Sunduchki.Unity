using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;

namespace Modules.AuthenticationSystem.Providers
{
    public class AnonymousCredentialProvider: ICredentialProvider
    {
        public AuthType AuthType => AuthType.Anonymous;
        public UniTask<AuthCredential> AcquireCredentialAsync()
        {
            return UniTask.FromResult(AuthCredentialBuilder.For(AuthType).Build());
        }
    }
}
