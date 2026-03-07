using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.Lobby.Services;
using R3;
using UnityEngine;
using Zenject;

namespace Modules.Lobby.Rules
{
    public sealed class LobbyConnectionRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly List<ILobbyConnectionHandler> _handlers;
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _cts;
        private bool _connected;
        private bool _connecting;
        private bool _retryScheduled;
        private string _trackedGameId = string.Empty;
        private string _trackedPlayerId = string.Empty;

        public LobbyConnectionRule(LobbyService lobbyService, List<ILobbyConnectionHandler> handlers)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _handlers = (handlers ?? new List<ILobbyConnectionHandler>())
                .OrderBy(GetHandlerPriority)
                .ThenBy(handler => handler?.GetType().FullName, StringComparer.Ordinal)
                .ToList();
        }

        public void Initialize()
        {
            _lobbyService.State
                .Subscribe(OnLobbyStateChanged)
                .AddTo(_disposables);
        }

        private void OnLobbyStateChanged(LobbyState state)
        {
            // Keep module handlers connected as soon as lobby transport is established,
            // so they are subscribed before game-start bursts (cards/deck/turn events).
            if (ShouldBeConnected(state))
            {
                if (ShouldConnect())
                {
                    BeginConnect().Forget();
                }

                return;
            }

            // Disconnect only when session is no longer active.
            if (!_connected)
            {
                return;
            }

            if (!ShouldBeConnected(state))
            {
                DisconnectAll().Forget();
            }
        }

        private static bool ShouldBeConnected(LobbyState state)
        {
            if (state.Started || state.Status == LobbyStatus.Started)
            {
                return true;
            }

            return state.Status == LobbyStatus.Connecting ||
                   state.Status == LobbyStatus.Waiting ||
                   state.Status == LobbyStatus.Starting;
        }

        private static int GetHandlerPriority(ILobbyConnectionHandler handler)
        {
            var typeName = handler?.GetType().Name ?? string.Empty;
            if (typeName.IndexOf("PlayerHand", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0;
            }

            if (typeName.IndexOf("Deck", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Turn", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 1;
            }

            return 10;
        }

        private bool ShouldConnect()
        {
            if (_connecting)
            {
                return false;
            }

            if (!_lobbyService.TryGetSession(out var gameId, out var playerId))
            {
                return false;
            }

            if (!_connected)
            {
                return true;
            }

            return !string.Equals(_trackedGameId, gameId, StringComparison.Ordinal) ||
                   !string.Equals(_trackedPlayerId, playerId, StringComparison.Ordinal);
        }

        private async UniTaskVoid BeginConnect()
        {
            if (_connecting)
            {
                return;
            }

            _connecting = true;
            if (!_lobbyService.TryGetSession(out var gameId, out var playerId))
            {
                _connecting = false;
                Debug.LogWarning("[Lobby] Missing game/player id, skipping module connect.");
                return;
            }

            await DisconnectAll();

            _cts = new CancellationTokenSource();
            var session = new LobbySession(gameId, playerId);
            try
            {
                foreach (var handler in _handlers)
                {
                    var handlerName = handler.GetType().Name;
                    Debug.Log($"[Lobby] Connecting handler: {handlerName}");
                    await handler.ConnectAsync(session, _cts.Token);
                    Debug.Log($"[Lobby] Connected handler: {handlerName}");
                }

                _connected = true;
                _trackedGameId = session.GameId;
                _trackedPlayerId = session.PlayerId;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Lobby] Failed to connect module handlers: {ex.Message}");
                _connected = false;
                _trackedGameId = string.Empty;
                _trackedPlayerId = string.Empty;
                ScheduleReconnectRetry();
            }
            finally
            {
                _connecting = false;
            }
        }

        private async UniTask DisconnectAll()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            foreach (var handler in _handlers)
            {
                await handler.DisconnectAsync();
            }

            _connected = false;
            _trackedGameId = string.Empty;
            _trackedPlayerId = string.Empty;
        }

        private void ScheduleReconnectRetry()
        {
            if (_retryScheduled)
            {
                return;
            }

            _retryScheduled = true;
            UniTask.Void(async () =>
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1));
                    var state = _lobbyService.State.CurrentValue;
                    if (!ShouldBeConnected(state))
                    {
                        return;
                    }

                    if (ShouldConnect())
                    {
                        BeginConnect().Forget();
                    }
                }
                finally
                {
                    _retryScheduled = false;
                }
            });
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
