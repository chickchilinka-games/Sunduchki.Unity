#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Modules.SignalR
{
    internal sealed class DotNetSignalRConnection : ISignalRConnection
    {
        private readonly HubConnection _connection;
        private readonly Dictionary<string, IDisposable> _subscriptions = new();
        private readonly SynchronizationContext _unityContext;
        private Action<Exception> _closedHandler;
        private Action<string> _reconnectedHandler;

        public DotNetSignalRConnection(HubConnection connection, SynchronizationContext unityContext)
        {
            _connection = connection;
            _unityContext = unityContext;
            _connection.Closed += OnConnectionClosedAsync;
            _connection.Reconnected += OnConnectionReconnectedAsync;
        }

        public UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            return UniTask.RunOnThreadPool(async () =>
            {
                await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
            });
        }

        public UniTask InvokeAsync(string methodName, object payload, CancellationToken cancellationToken = default)
        {
            var args = WrapPayload(payload);
            return UniTask.RunOnThreadPool(async () =>
            {
                await _connection.InvokeCoreAsync<object>(methodName, args, cancellationToken).ConfigureAwait(false);
            });
        }

        public void On(string methodName, Action handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On(methodName, () => PostToMainThread(handler));
        }

        public void On<T>(string methodName, Action<T> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On<T>(methodName, arg =>
            {
                PostToMainThread(() => handler(arg));
            });
        }

        public void On<T1, T2>(string methodName, Action<T1, T2> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On<T1, T2>(methodName, (arg1, arg2) =>
            {
                PostToMainThread(() => handler(arg1, arg2));
            });
        }

        public void On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On<T1, T2, T3>(methodName, (arg1, arg2, arg3) =>
            {
                PostToMainThread(() => handler(arg1, arg2, arg3));
            });
        }

        public void On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handler)
        {
            RemoveHandler(methodName);
            _subscriptions[methodName] = _connection.On<T1, T2, T3, T4>(methodName, (arg1, arg2, arg3, arg4) =>
            {
                PostToMainThread(() => handler(arg1, arg2, arg3, arg4));
            });
        }

        public void OnClosed(Action<Exception> handler)
        {
            _closedHandler = handler;
        }

        public void OnReconnected(Action<string> handler)
        {
            _reconnectedHandler = handler;
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
            return UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    await _connection.StopAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                }
            });
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

        private void PostToMainThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (_unityContext == null || SynchronizationContext.Current == _unityContext)
            {
                action();
                return;
            }

            _unityContext.Post(_ => action(), null);
        }

        private Task OnConnectionClosedAsync(Exception exception)
        {
            if (_closedHandler == null)
            {
                return Task.CompletedTask;
            }

            PostToMainThread(() => _closedHandler?.Invoke(exception));
            return Task.CompletedTask;
        }

        private Task OnConnectionReconnectedAsync(string connectionId)
        {
            if (_reconnectedHandler == null)
            {
                return Task.CompletedTask;
            }

            PostToMainThread(() => _reconnectedHandler?.Invoke(connectionId));
            return Task.CompletedTask;
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
        public void OnClosed(Action<Exception> handler) => throw new PlatformNotSupportedException();
        public void OnReconnected(Action<string> handler) => throw new PlatformNotSupportedException();
        public void RemoveHandler(string methodName) => throw new PlatformNotSupportedException();
        public UniTask StopAsync(CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public void Dispose() { }
    }
}
#endif
