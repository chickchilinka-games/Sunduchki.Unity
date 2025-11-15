using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DeckSystem.Data;

namespace Modules.DeckSystem.Interfaces
{
    public interface IDeckSignalClient
    {
        UniTask<IDisposable> SubscribeAsync(DeckTrackingOptions options, IDeckSignalListener listener, CancellationToken cancellationToken = default);
    }
}
