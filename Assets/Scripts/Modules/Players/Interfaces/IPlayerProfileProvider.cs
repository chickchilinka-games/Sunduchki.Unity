using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Players.Data;

namespace Modules.Players.Interfaces
{
    public interface IPlayerProfileProvider
    {
        UniTask<PlayerProfile?> GetProfileAsync(string playerId, CancellationToken cancellationToken = default);
    }
}
