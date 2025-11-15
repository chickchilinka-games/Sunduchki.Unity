using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;

namespace Modules.AuthenticationSystem.Interfaces
{
    public interface IIdentityAuthority
    {
        UniTask<UserContext> GetCachedUserContextAsync();

        UniTask<UserContext> SignInAsync(AuthCredential credential);

        UniTask<LinkageInfo> LinkAsync(AuthCredential credential);

        UniTask UnlinkAsync(AuthType authType);

        UniTask SignOutAsync();
    }
}
