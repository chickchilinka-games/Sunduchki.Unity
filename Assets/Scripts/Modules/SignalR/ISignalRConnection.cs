using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.SignalR
{
    public interface ISignalRConnection : IDisposable
    {
        UniTask StartAsync(CancellationToken cancellationToken = default);
        UniTask InvokeAsync(string methodName, object payload, CancellationToken cancellationToken = default);
        void On(string methodName, Action handler);
        void On<T>(string methodName, Action<T> handler);
        void On<T1, T2>(string methodName, Action<T1, T2> handler);
        void On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler);
        void On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handler);
        void OnClosed(Action<Exception> handler);
        void OnReconnected(Action<string> handler);
        void RemoveHandler(string methodName);
        UniTask StopAsync(CancellationToken cancellationToken = default);
    }
}
