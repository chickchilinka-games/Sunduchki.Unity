using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;

namespace Modules.AuthenticationSystem.Interfaces
{
    public interface ICredentialProvider
    {
        AuthType AuthType { get; }

        UniTask<AuthCredential> AcquireCredentialAsync();
    }
}
