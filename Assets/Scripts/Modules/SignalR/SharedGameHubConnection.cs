using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.SignalR.Config;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Modules.SignalR
{
    public sealed class SharedGameHubConnection : ISharedGameHubConnection, IDisposable
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly object _subscriptionsSync = new();
        private readonly object _lifecycleSync = new();
        private readonly Dictionary<string, MethodSubscriptions> _subscriptions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HandlerKinds> _registeredMethods = new(StringComparer.Ordinal);
        private readonly List<Action<string>> _closedHandlers = new();
        private readonly List<Action> _reconnectedHandlers = new();

        private ISignalRConnection _connection;
        private int _acquireCount;
        private bool _disposed;
        private bool _suppressClosed;
        private string _sessionGameId = string.Empty;
        private string _sessionPlayerId = string.Empty;
        private string _joinedGameId = string.Empty;
        private string _joinedPlayerId = string.Empty;

        public SharedGameHubConnection(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public async UniTask AcquireAsync(
            string gameId,
            string playerId,
            CancellationToken cancellationToken = default,
            bool forceJoin = false)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
            {
                Debug.LogWarning("[SignalR.Shared] Acquire called with empty game or player id.");
                return;
            }

            await _gate.WaitAsync(cancellationToken);
            try
            {
                ThrowIfDisposed();

                if (_connection == null)
                {
                    var connection = _connectionFactory.Create(_configProvider.GetHubUri(), _configProvider.GetAccessToken());
                    connection.OnClosed(OnClosed);
                    connection.OnReconnected(OnReconnected);
                    RegisterAllHandlers(connection);
                    await connection.StartAsync(cancellationToken);
                    _connection = connection;
                    _joinedGameId = string.Empty;
                    _joinedPlayerId = string.Empty;
                }

                _sessionGameId = gameId;
                _sessionPlayerId = playerId;

                if (forceJoin || !IsJoinedSession(gameId, playerId))
                {
                    await JoinCurrentSessionAsync(cancellationToken);
                }

                _acquireCount++;
            }
            catch
            {
                if (_connection != null && _acquireCount == 0)
                {
                    await DisconnectCoreAsync();
                }

                throw;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async UniTask ReleaseAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (_acquireCount > 0)
                {
                    _acquireCount--;
                }

                if (_acquireCount == 0)
                {
                    await DisconnectCoreAsync();
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public IDisposable Subscribe(string methodName, Action handler)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Handler method name is required.", nameof(methodName));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            ThrowIfDisposed();
            var entry = new NoArgSubscriptionEntry(handler);

            lock (_subscriptionsSync)
            {
                var subscriptions = GetOrCreateMethodSubscriptions(methodName);
                subscriptions.NoArgEntries.Add(entry);

                if (_connection != null)
                {
                    EnsureRegistrationMode(_connection, methodName, subscriptions.RequiredKinds);
                }
            }

            return new CallbackDisposable(() => UnsubscribeNoArg(methodName, entry));
        }

        public IDisposable Subscribe<T>(string methodName, Action<T> handler)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Handler method name is required.", nameof(methodName));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            ThrowIfDisposed();
            var entry = new PayloadSubscriptionEntry(payload =>
            {
                if (!TryConvertPayload(payload, out T mapped))
                {
                    var payloadType = payload?.GetType().FullName ?? "null";
                    Debug.LogWarning(
                        $"[SignalR.Shared] Failed to map '{methodName}' payload type '{payloadType}' to {typeof(T).Name}.");
                    return;
                }

                handler(mapped);
            });

            lock (_subscriptionsSync)
            {
                var subscriptions = GetOrCreateMethodSubscriptions(methodName);
                subscriptions.PayloadEntries.Add(entry);

                if (_connection != null)
                {
                    EnsureRegistrationMode(_connection, methodName, subscriptions.RequiredKinds);
                }
            }

            return new CallbackDisposable(() => Unsubscribe(methodName, entry));
        }

        public IDisposable SubscribeClosed(Action<string> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            ThrowIfDisposed();
            lock (_lifecycleSync)
            {
                _closedHandlers.Add(handler);
            }

            return new CallbackDisposable(() =>
            {
                lock (_lifecycleSync)
                {
                    _closedHandlers.Remove(handler);
                }
            });
        }

        public IDisposable SubscribeReconnected(Action handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            ThrowIfDisposed();
            lock (_lifecycleSync)
            {
                _reconnectedHandlers.Add(handler);
            }

            return new CallbackDisposable(() =>
            {
                lock (_lifecycleSync)
                {
                    _reconnectedHandlers.Remove(handler);
                }
            });
        }

        public async UniTask InvokeAsync(string methodName, object payload, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Method name is required.", nameof(methodName));
            }

            ThrowIfDisposed();
            var connection = _connection;
            if (connection == null)
            {
                throw new InvalidOperationException("Shared SignalR connection is not established.");
            }

            await connection.InvokeAsync(methodName, payload, cancellationToken);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            UniTask.Void(async () =>
            {
                await _gate.WaitAsync();
                try
                {
                    await DisconnectCoreAsync();
                }
                finally
                {
                    _gate.Release();
                    _gate.Dispose();
                }
            });
        }

        private void RegisterAllHandlers(ISignalRConnection connection)
        {
            lock (_subscriptionsSync)
            {
                _registeredMethods.Clear();
                foreach (var pair in _subscriptions)
                {
                    EnsureRegistrationMode(connection, pair.Key, pair.Value.RequiredKinds);
                }
            }
        }

        private void RegisterHandler(ISignalRConnection connection, string methodName, HandlerKinds kinds)
        {
            if ((kinds & HandlerKinds.NoArgs) != 0)
            {
                connection.On(methodName, () => DispatchNoArgs(methodName));
            }

            if ((kinds & HandlerKinds.Payload) != 0)
            {
                connection.On<object>(methodName, payload => DispatchPayload(methodName, payload));
            }
        }

        private void DispatchNoArgs(string methodName)
        {
            List<NoArgSubscriptionEntry> handlersCopy = null;
            lock (_subscriptionsSync)
            {
                if (!_subscriptions.TryGetValue(methodName, out var handlers) || handlers.NoArgEntries.Count == 0)
                {
                    return;
                }

                handlersCopy = new List<NoArgSubscriptionEntry>(handlers.NoArgEntries);
            }

            foreach (var entry in handlersCopy)
            {
                try
                {
                    entry.Callback();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SignalR.Shared] Handler '{methodName}' failed: {ex.Message}");
                }
            }
        }

        private void DispatchPayload(string methodName, object payload)
        {
            List<PayloadSubscriptionEntry> handlersCopy = null;
            lock (_subscriptionsSync)
            {
                if (!_subscriptions.TryGetValue(methodName, out var handlers) || handlers.PayloadEntries.Count == 0)
                {
                    return;
                }

                handlersCopy = new List<PayloadSubscriptionEntry>(handlers.PayloadEntries);
            }

            foreach (var entry in handlersCopy)
            {
                try
                {
                    entry.Callback(payload);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SignalR.Shared] Handler '{methodName}' failed: {ex.Message}");
                }
            }
        }

        private void UnsubscribeNoArg(string methodName, NoArgSubscriptionEntry entry)
        {
            lock (_subscriptionsSync)
            {
                if (!_subscriptions.TryGetValue(methodName, out var subscriptions))
                {
                    return;
                }

                subscriptions.NoArgEntries.Remove(entry);
                UpdateRegistrationAfterChange(methodName, subscriptions);
            }
        }

        private void Unsubscribe(string methodName, PayloadSubscriptionEntry entry)
        {
            lock (_subscriptionsSync)
            {
                if (!_subscriptions.TryGetValue(methodName, out var subscriptions))
                {
                    return;
                }

                subscriptions.PayloadEntries.Remove(entry);
                UpdateRegistrationAfterChange(methodName, subscriptions);
            }
        }

        private MethodSubscriptions GetOrCreateMethodSubscriptions(string methodName)
        {
            if (_subscriptions.TryGetValue(methodName, out var subscriptions))
            {
                return subscriptions;
            }

            subscriptions = new MethodSubscriptions();
            _subscriptions[methodName] = subscriptions;
            return subscriptions;
        }

        private void UpdateRegistrationAfterChange(string methodName, MethodSubscriptions subscriptions)
        {
            if (subscriptions.IsEmpty)
            {
                _subscriptions.Remove(methodName);
                if (_connection != null && _registeredMethods.Remove(methodName))
                {
                    _connection.RemoveHandler(methodName);
                }

                return;
            }

            if (_connection != null)
            {
                EnsureRegistrationMode(_connection, methodName, subscriptions.RequiredKinds);
            }
        }

        private void EnsureRegistrationMode(ISignalRConnection connection, string methodName, HandlerKinds requiredKinds)
        {
            if (requiredKinds == HandlerKinds.None)
            {
                if (_registeredMethods.Remove(methodName))
                {
                    connection.RemoveHandler(methodName);
                }

                return;
            }

            if (_registeredMethods.TryGetValue(methodName, out var currentKinds) && currentKinds == requiredKinds)
            {
                return;
            }

            if (_registeredMethods.ContainsKey(methodName))
            {
                connection.RemoveHandler(methodName);
            }

            RegisterHandler(connection, methodName, requiredKinds);
            _registeredMethods[methodName] = requiredKinds;
        }

        private async UniTask JoinCurrentSessionAsync(CancellationToken cancellationToken = default)
        {
            if (_connection == null ||
                string.IsNullOrWhiteSpace(_sessionGameId) ||
                string.IsNullOrWhiteSpace(_sessionPlayerId))
            {
                return;
            }

            await _connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = _sessionGameId,
                PlayerId = _sessionPlayerId
            }, cancellationToken);

            _joinedGameId = _sessionGameId;
            _joinedPlayerId = _sessionPlayerId;
        }

        private async UniTask DisconnectCoreAsync()
        {
            var connection = _connection;
            _connection = null;
            _sessionGameId = string.Empty;
            _sessionPlayerId = string.Empty;
            _joinedGameId = string.Empty;
            _joinedPlayerId = string.Empty;

            lock (_subscriptionsSync)
            {
                _registeredMethods.Clear();
            }

            if (connection == null)
            {
                return;
            }

            try
            {
                _suppressClosed = true;
                await connection.StopAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SignalR.Shared] Stop failed: {ex.Message}");
            }
            finally
            {
                _suppressClosed = false;
                connection.Dispose();
            }
        }

        private void OnClosed(Exception error)
        {
            if (!_suppressClosed)
            {
                NotifyClosed(error?.Message ?? string.Empty);
            }

            _joinedGameId = string.Empty;
            _joinedPlayerId = string.Empty;
        }

        private void OnReconnected(string _)
        {
            UniTask.Void(async () =>
            {
                await _gate.WaitAsync();
                try
                {
                    if (_connection == null)
                    {
                        return;
                    }

                    try
                    {
                        await JoinCurrentSessionAsync();
                        NotifyReconnected();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SignalR.Shared] Rejoin after reconnect failed: {ex.Message}");
                        NotifyClosed($"Rejoin failed: {ex.Message}");
                    }
                }
                finally
                {
                    _gate.Release();
                }
            });
        }

        private void NotifyClosed(string error)
        {
            List<Action<string>> handlersCopy;
            lock (_lifecycleSync)
            {
                if (_closedHandlers.Count == 0)
                {
                    return;
                }

                handlersCopy = new List<Action<string>>(_closedHandlers);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    handler(error);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SignalR.Shared] Closed handler failed: {ex.Message}");
                }
            }
        }

        private void NotifyReconnected()
        {
            List<Action> handlersCopy;
            lock (_lifecycleSync)
            {
                if (_reconnectedHandlers.Count == 0)
                {
                    return;
                }

                handlersCopy = new List<Action>(_reconnectedHandlers);
            }

            foreach (var handler in handlersCopy)
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SignalR.Shared] Reconnected handler failed: {ex.Message}");
                }
            }
        }

        private bool IsJoinedSession(string gameId, string playerId)
        {
            return string.Equals(_joinedGameId, gameId, StringComparison.Ordinal) &&
                   string.Equals(_joinedPlayerId, playerId, StringComparison.Ordinal);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SharedGameHubConnection));
            }
        }

        private static bool TryConvertPayload<T>(object payload, out T value)
        {
            if (payload is T typed)
            {
                value = typed;
                return true;
            }

            if (payload == null)
            {
                value = default;
                return true;
            }

            if (payload is object[] array && array.Length == 1)
            {
                return TryConvertPayload(array[0], out value);
            }

            if (TryConvertFromJsonElement(payload, out value))
            {
                return true;
            }

            if (payload is string json)
            {
                if (typeof(T) == typeof(string))
                {
                    value = (T)(object)json;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        var parsed = JToken.Parse(json);
                        value = parsed.ToObject<T>();
                        return true;
                    }
                    catch
                    {
                        // Fallback to generic conversion below.
                    }
                }
            }

            try
            {
                var token = payload as JToken ?? JToken.FromObject(payload);
                value = token.ToObject<T>();
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        private static bool TryConvertFromJsonElement<T>(object payload, out T value)
        {
            var payloadType = payload?.GetType();
            if (payloadType == null || !string.Equals(payloadType.FullName, "System.Text.Json.JsonElement", StringComparison.Ordinal))
            {
                value = default;
                return false;
            }

            var rawText = InvokeStringMethod(payload, "GetRawText");
            if (!string.IsNullOrWhiteSpace(rawText))
            {
                try
                {
                    var rawToken = JToken.Parse(rawText);
                    value = rawToken.ToObject<T>();
                    return true;
                }
                catch
                {
                    // Fallback to non-JSON string conversion below.
                }
            }

            var text = payload.ToString() ?? string.Empty;
            if (typeof(T) == typeof(string))
            {
                value = (T)(object)text;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    var token = JToken.Parse(text);
                    value = token.ToObject<T>();
                    return true;
                }
                catch
                {
                    // Final conversion fallback in caller.
                }
            }

            value = default;
            return false;
        }

        private static string InvokeStringMethod(object instance, string methodName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(methodName))
            {
                return string.Empty;
            }

            try
            {
                var method = instance.GetType().GetMethod(methodName, Type.EmptyTypes);
                var result = method?.Invoke(instance, null);
                return result as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }

        private sealed class PayloadSubscriptionEntry
        {
            public Action<object> Callback { get; }

            public PayloadSubscriptionEntry(Action<object> callback)
            {
                Callback = callback;
            }
        }

        private sealed class NoArgSubscriptionEntry
        {
            public Action Callback { get; }

            public NoArgSubscriptionEntry(Action callback)
            {
                Callback = callback;
            }
        }

        private sealed class MethodSubscriptions
        {
            public List<NoArgSubscriptionEntry> NoArgEntries { get; } = new();
            public List<PayloadSubscriptionEntry> PayloadEntries { get; } = new();
            public bool IsEmpty => NoArgEntries.Count == 0 && PayloadEntries.Count == 0;

            public HandlerKinds RequiredKinds
            {
                get
                {
                    var kinds = HandlerKinds.None;
                    if (NoArgEntries.Count > 0)
                    {
                        kinds |= HandlerKinds.NoArgs;
                    }

                    if (PayloadEntries.Count > 0)
                    {
                        kinds |= HandlerKinds.Payload;
                    }

                    return kinds;
                }
            }
        }

        [Flags]
        private enum HandlerKinds
        {
            None = 0,
            NoArgs = 1,
            Payload = 2
        }

        private sealed class CallbackDisposable : IDisposable
        {
            private Action _callback;

            public CallbackDisposable(Action callback)
            {
                _callback = callback;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _callback, null)?.Invoke();
            }
        }
    }
}
