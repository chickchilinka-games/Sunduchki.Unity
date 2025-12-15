using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Data;

namespace Modules.BonusSystem.Interfaces
{
    public interface IBonusActionClient
    {
        UniTask ConnectAsync(BonusConnectionOptions options, CancellationToken cancellationToken = default);

        UniTask DisconnectAsync();

        UniTask UseBonusAsync(BonusUsePayload payload, CancellationToken cancellationToken = default);
    }
}
