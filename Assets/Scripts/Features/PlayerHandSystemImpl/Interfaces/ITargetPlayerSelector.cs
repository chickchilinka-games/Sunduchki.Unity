using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.PlayerHandSystemImpl.Interfaces
{
    public interface ITargetPlayerSelector
    {
        UniTask<string> SelectAsync(CancellationToken cancellationToken = default);
    }
}
