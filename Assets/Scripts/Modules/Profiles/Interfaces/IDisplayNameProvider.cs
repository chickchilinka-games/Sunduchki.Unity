using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;

namespace Modules.Profiles.Interfaces
{
    public interface IDisplayNameProvider
    {
        UniTask<OperationResult> ChangeDisplayNameAsync(string displayName, CancellationToken cancellationToken = default);
    }
}
