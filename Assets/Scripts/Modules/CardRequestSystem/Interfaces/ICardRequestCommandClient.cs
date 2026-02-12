using System.Threading;
using Cysharp.Threading.Tasks;
namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestCommandClient
    {
        UniTask AskAsync(string rank, string targetPlayerId, CancellationToken cancellationToken = default);
    }
}
