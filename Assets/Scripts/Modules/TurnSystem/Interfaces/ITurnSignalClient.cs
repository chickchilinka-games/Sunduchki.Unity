using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.TurnSystem.Data;

namespace Modules.TurnSystem.Interfaces
{
    public interface ITurnSignalClient
    {
        UniTask<IDisposable> SubscribeAsync(TurnTrackingOptions options, Action<string> onTurnAdvanced, CancellationToken cancellationToken = default);
    }
}
