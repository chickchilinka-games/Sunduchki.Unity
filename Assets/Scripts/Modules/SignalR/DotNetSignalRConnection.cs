#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Modules.SignalR
{
    internal sealed class DotNetSignalRConnection : ISignalRConnection
    {
        private readonly HubConnection _connection;
        private readonly Dictionary<string, IDisposable> _subscriptions = new();

        public DotNetSignalRConnection(HubConnection connection)
        {
            _connection = connection;
        }

        public UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            return _connection.StartAsync(cancellationToken).AsUniTask();
        }

        public UniTask InvokeAsync(string methodName, object payload, CancellationToken cancellationToken = default)
        {
            var args = WrapPayload(payload);
            return _connection.InvokeCoreAsync<object>(methodName, args, cancellationToken).AsUniTask();
        }

        public void On(string methodName, Action handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On(methodName, handler);
        }

        public void On<T>(string methodName, Action<T> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On(methodName, handler);
        }

        public void On<T1, T2>(string methodName, Action<T1, T2> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On(methodName, handler);
        }

        public void On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On(methodName, handler);
        }

        public void On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On(methodName, handler);
        }

        public void RemoveHandler(string methodName)
        {
            if (_subscriptions.TryGetValue(methodName, out var disposable))
            {
                disposable.Dispose();
                _subscriptions.Remove(methodName);
            }
        }

        public UniTask StopAsync(CancellationToken cancellationToken = default)
        {
            return _connection.StopAsync(cancellationToken).AsUniTask();
        }

        public void Dispose()
        {
            foreach (var disposable in _subscriptions.Values)
            {
                disposable.Dispose();
            }
            _subscriptions.Clear();
            _connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static object[] WrapPayload(object payload)
        {
            if (payload == null)
            {
                return Array.Empty<object>();
            }

            if (payload is object[] array)
            {
                return array;
            }

            if (payload is IEnumerable<object> genericEnumerable)
            {
                return genericEnumerable.ToArray();
            }

            if (payload is IEnumerable enumerable && payload is not string)
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    list.Add(item);
                }

                return list.ToArray();
            }

            return new[] { payload };
        }
    }
}
#else
namespace Modules.SignalR
{
    internal sealed class DotNetSignalRConnection : ISignalRConnection
    {
        public UniTask StartAsync(CancellationToken cancellationToken = default) => UniTask.FromException(new PlatformNotSupportedException());
        public UniTask InvokeAsync(string methodName, object payload, CancellationToken cancellationToken = default) => UniTask.FromException(new PlatformNotSupportedException());
        public void On(string methodName, Action handler) => throw new PlatformNotSupportedException();
        public void On<T>(string methodName, Action<T> handler) => throw new PlatformNotSupportedException();
        public void On<T1, T2>(string methodName, Action<T1, T2> handler) => throw new PlatformNotSupportedException();
        public void On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler) => throw new PlatformNotSupportedException();
        public void On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handler) => throw new PlatformNotSupportedException();
        public void RemoveHandler(string methodName) => throw new PlatformNotSupportedException();
        public UniTask StopAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public void Dispose() { }
    }
}
#endif
