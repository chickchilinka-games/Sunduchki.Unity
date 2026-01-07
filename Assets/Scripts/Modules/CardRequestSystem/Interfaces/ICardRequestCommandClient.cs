using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;

namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestCommandClient
    {
        UniTask ConnectAsync(CardRequestCommandOptions options, CancellationToken cancellationToken = default);
        UniTask DisconnectAsync();
        UniTask AskAsync(CardRequestAskPayload payload, CancellationToken cancellationToken = default);
    }
}
