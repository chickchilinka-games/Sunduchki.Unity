using System.Threading;
using Cysharp.Threading.Tasks;
namespace Modules.BonusSystem.Interfaces
{
    public interface IBonusActionClient
    {
        UniTask UseBonusAsync(string bonusType, string targetPlayerId, CancellationToken cancellationToken = default);
    }
}
