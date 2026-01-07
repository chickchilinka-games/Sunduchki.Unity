using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Profiles.Data;

namespace Modules.Profiles.Interfaces
{
    public interface IProfileApiClient
    {
        UniTask<ProfileInfo> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    }
}
