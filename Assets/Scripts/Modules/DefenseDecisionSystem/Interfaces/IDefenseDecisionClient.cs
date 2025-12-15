using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;

namespace Modules.DefenseDecisionSystem.Interfaces
{
    public interface IDefenseDecisionClient
    {
        UniTask ConnectAsync(DefenseDecisionConnectionOptions options, CancellationToken cancellationToken = default);
        UniTask SubmitDecisionAsync(DefenseDecisionSubmitPayload payload, CancellationToken cancellationToken = default);
        UniTask DisconnectAsync();
    }
}
