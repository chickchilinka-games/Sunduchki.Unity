using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.PlayerHand.Data;

namespace Modules.PlayerHand.Interfaces
{
    public interface IPlayerHandSignalClient
    {
        UniTask<IDisposable> SubscribeAsync(PlayerHandTrackingOptions options, IPlayerHandSignalListener listener, CancellationToken cancellationToken = default);
    }
}
