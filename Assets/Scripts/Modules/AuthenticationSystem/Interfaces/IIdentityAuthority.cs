using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;

namespace Modules.AuthenticationSystem.Interfaces
{
    public interface IIdentityAuthority
    {
        UniTask<UserContext> GetCachedUserContextAsync();

        UniTask<UserContext> SignInAsync(AuthCredential credential);

        UniTask<LinkageInfo> LinkAsync(AuthCredential credential);

        UniTask DeleteAsync(AuthCredential credential);

        UniTask UnlinkAsync(AuthType authType);

        UniTask SignOutAsync();

        UniTask<UserContext> RefreshAsync(CancellationToken cancellationToken = default);
    }
}
