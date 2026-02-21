using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if UNITY_WEBGL && !UNITY_EDITOR
namespace Modules.SignalR
{
    internal sealed class WebGLSignalRConnection : ISignalRConnection, IWebGLSignalRManagedConnection
    {
        private readonly WebGLSignalRBridgeHost _host;
        private readonly int _connectionId;

        private UniTaskCompletionSource _startTcs;
        private UniTaskCompletionSource _stopTcs;
        private bool _disposed;
        private Action<Exception> _closedHandler;
        private Action<string> _reconnectedHandler;
        private bool _hasStarted;
        private const float StartTimeoutSeconds = 20f;
        private const float StopTimeoutSeconds = 2f;

        private readonly Dictionary<string, Action<string>> _rawHandlers = new();
        private readonly Dictionary<string, UniTaskCompletionSource> _pendingInvokes = new();

        public WebGLSignalRConnection(WebGLSignalRBridgeHost host, int connectionId)
        {
            _host = host;
            _connectionId = connectionId;
        }

        public UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            _startTcs = new UniTaskCompletionSource();
            _host.StartConnection(_connectionId);
            return WaitStartAsync(cancellationToken);
        }

        public UniTask InvokeAsync(string methodName, object payload, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var requestId = Guid.NewGuid().ToString("N");
            var tcs = new UniTaskCompletionSource();
            _pendingInvokes[requestId] = tcs;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => tcs.TrySetCanceled());
            }

            var argsJson = SerializePayload(payload);
            _host.Invoke(_connectionId, requestId, methodName, argsJson);
            return tcs.Task;
        }

        public void On(string methodName, Action handler)
        {
            RegisterHandler(methodName, _ => handler?.Invoke());
        }

        public void On<T>(string methodName, Action<T> handler)
        {
            RegisterHandler(methodName, argsJson =>
            {
                var args = ParseArgs(argsJson);
                var value = args.Count > 0 ? args[0].ToObject<T>() : default;
                handler?.Invoke(value);
            });
        }

        public void On<T1, T2>(string methodName, Action<T1, T2> handler)
        {
            RegisterHandler(methodName, argsJson =>
            {
                var args = ParseArgs(argsJson);
                var first = args.Count > 0 ? args[0].ToObject<T1>() : default;
                var second = args.Count > 1 ? args[1].ToObject<T2>() : default;
                handler?.Invoke(first, second);
            });
        }

        public void On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler)
        {
            RegisterHandler(methodName, argsJson =>
            {
                var args = ParseArgs(argsJson);
                var first = args.Count > 0 ? args[0].ToObject<T1>() : default;
                var second = args.Count > 1 ? args[1].ToObject<T2>() : default;
                var third = args.Count > 2 ? args[2].ToObject<T3>() : default;
                handler?.Invoke(first, second, third);
            });
        }

        public void On<T1, T2, T3, T4>(string methodName, Action<T1, T2, T3, T4> handler)
        {
            RegisterHandler(methodName, argsJson =>
            {
                var args = ParseArgs(argsJson);
                var first = args.Count > 0 ? args[0].ToObject<T1>() : default;
                var second = args.Count > 1 ? args[1].ToObject<T2>() : default;
                var third = args.Count > 2 ? args[2].ToObject<T3>() : default;
                var fourth = args.Count > 3 ? args[3].ToObject<T4>() : default;
                handler?.Invoke(first, second, third, fourth);
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
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return;
            }

            if (_rawHandlers.Remove(methodName))
            {
                _host.UnregisterHandler(_connectionId, methodName);
            }
        }

        public UniTask StopAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return UniTask.CompletedTask;
            }

            _stopTcs = new UniTaskCompletionSource();
            _host.StopConnection(_connectionId);
            return WaitStopAsync(cancellationToken);
        }

        public void HandleMessage(SignalRMessageEnvelope envelope)
        {
            if (envelope == null)
            {
                return;
            }

            switch (envelope.Type)
            {
                case "started":
                    _startTcs?.TrySetResult();
                    if (_hasStarted)
                    {
                        _reconnectedHandler?.Invoke(null);
                    }
                    else
                    {
                        _hasStarted = true;
                    }
                    break;
                case "reconnected":
                    _reconnectedHandler?.Invoke(envelope.RequestId);
                    break;
                case "startFailed":
                    _startTcs?.TrySetException(new InvalidOperationException(envelope.Error ?? "SignalR start failed."));
                    break;
                case "handler":
                    if (!string.IsNullOrEmpty(envelope.Handler) && _rawHandlers.TryGetValue(envelope.Handler, out var callback))
                    {
                        callback?.Invoke(envelope.ArgsJson ?? "[]");
                    }
                    break;
                case "invokeResult":
                    if (!string.IsNullOrEmpty(envelope.RequestId) && _pendingInvokes.TryGetValue(envelope.RequestId, out var invokeTcs))
                    {
                        if (envelope.Success)
                        {
                            invokeTcs.TrySetResult();
                        }
                        else
                        {
                            invokeTcs.TrySetException(new InvalidOperationException(envelope.Error ?? "Invoke failed."));
                        }

                        _pendingInvokes.Remove(envelope.RequestId);
                    }
                    break;
                case "stopped":
                case "closed":
                    if (!_hasStarted && _startTcs != null)
                    {
                        var startError = string.IsNullOrWhiteSpace(envelope.Error)
                            ? "SignalR connection closed during start."
                            : envelope.Error;
                        _startTcs.TrySetException(new InvalidOperationException(startError));
                    }

                    _stopTcs?.TrySetResult();
                    if (envelope.Type == "closed" && _closedHandler != null)
                    {
                        var error = string.IsNullOrWhiteSpace(envelope.Error)
                            ? null
                            : new InvalidOperationException(envelope.Error);
                        _closedHandler.Invoke(error);
                    }
                    CleanupPending(new InvalidOperationException(envelope.Error ?? "Connection closed."));
                    _host.RemoveConnection(_connectionId);
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _host.StopConnection(_connectionId);
            }
            catch
            {
                // ignored
            }

            _host.RemoveConnection(_connectionId);
            CleanupPending(new OperationCanceledException("SignalR connection disposed."));
            _rawHandlers.Clear();
        }

        private void RegisterHandler(string handlerName, Action<string> callback)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(handlerName))
            {
                return;
            }

            _rawHandlers[handlerName] = callback;
            _host.RegisterHandler(_connectionId, handlerName);
        }

        private async UniTask WaitStartAsync(CancellationToken cancellationToken)
        {
            var startTask = _startTcs.Task;
            var winner = await UniTask.WhenAny(startTask, UniTask.Delay(TimeSpan.FromSeconds(StartTimeoutSeconds)));
            if (winner == 0)
            {
                await startTask.AttachExternalCancellation(cancellationToken);
                return;
            }

            try
            {
                _host.StopConnection(_connectionId);
            }
            catch
            {
                // ignore stop failures on timeout path
            }

            _host.RemoveConnection(_connectionId);
            throw new TimeoutException($"SignalR start timed out after {StartTimeoutSeconds} seconds.");
        }

        private async UniTask WaitStopAsync(CancellationToken cancellationToken)
        {
            var stopTask = _stopTcs.Task;
            var winner = await UniTask.WhenAny(stopTask, UniTask.Delay(TimeSpan.FromSeconds(StopTimeoutSeconds)));
            if (winner == 0)
            {
                await stopTask.AttachExternalCancellation(cancellationToken);
                return;
            }

            // Bridge can miss "stopped" (e.g. already-closed JS handle); don't block lifecycle.
            _stopTcs?.TrySetResult();
            _host.RemoveConnection(_connectionId);
        }

        private void CleanupPending(Exception error)
        {
            foreach (var handler in _pendingInvokes.Values)
            {
                handler.TrySetException(error);
            }

            _pendingInvokes.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebGLSignalRConnection));
            }
        }

        private static JArray ParseArgs(string argsJson)
        {
            if (string.IsNullOrWhiteSpace(argsJson))
            {
                return new JArray();
            }

            try
            {
                var token = JToken.Parse(argsJson);
                return token as JArray ?? new JArray(token);
            }
            catch
            {
                return new JArray();
            }
        }

        private static string SerializePayload(object payload)
        {
            if (payload == null)
            {
                return "[]";
            }

            if (payload is string)
            {
                return JsonConvert.SerializeObject(new object[] { payload });
            }

            if (payload is IEnumerable<object> enumerable)
            {
                return JsonConvert.SerializeObject(enumerable);
            }

            if (payload is System.Collections.IEnumerable nonGeneric)
            {
                var list = new List<object>();
                foreach (var item in nonGeneric)
                {
                    list.Add(item);
                }

                return JsonConvert.SerializeObject(list);
            }

            return JsonConvert.SerializeObject(new object[] { payload });
        }
    }
}
#else
namespace Modules.SignalR
{
    internal sealed class WebGLSignalRConnection : ISignalRConnection
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
