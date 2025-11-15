using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;

namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestSignalClient
    {
        UniTask<IDisposable> SubscribeAsync(CardRequestTrackingOptions options, ICardRequestSignalListener listener, CancellationToken cancellationToken = default);
    }
}
