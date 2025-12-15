using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Data;
using R3;

namespace Modules.BonusSystem.Interfaces
{
    public interface IBonusActionService
    {
        ReadOnlyReactiveProperty<bool> IsExecuting { get; }

        UniTask<bool> UseBonusAsync(BonusUseRequest request, CancellationToken cancellationToken = default);

        void Configure(string gameId, string playerId);

        void Reset();
    }
}
