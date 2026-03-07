using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.SignalR
{
    public interface ISharedGameHubConnection
    {
        UniTask AcquireAsync(
            string gameId,
            string playerId,
            CancellationToken cancellationToken = default,
            bool forceJoin = false);
        UniTask ReleaseAsync();
        IDisposable Subscribe(string methodName, Action handler);
        IDisposable Subscribe<T>(string methodName, Action<T> handler);
        IDisposable SubscribeClosed(Action<string> handler);
        IDisposable SubscribeReconnected(Action handler);
        UniTask InvokeAsync(string methodName, object payload, CancellationToken cancellationToken = default);
    }
}
