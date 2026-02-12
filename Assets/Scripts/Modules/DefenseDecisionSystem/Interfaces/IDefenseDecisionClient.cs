using System.Threading;
using Cysharp.Threading.Tasks;
namespace Modules.DefenseDecisionSystem.Interfaces
{
    public interface IDefenseDecisionClient
    {
        UniTask SubmitDecisionAsync(string targetPlayerId, bool useBonus, string bonusType, CancellationToken cancellationToken = default);
    }
}
