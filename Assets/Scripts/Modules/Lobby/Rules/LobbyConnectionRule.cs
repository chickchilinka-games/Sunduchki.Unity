using System;
using System.Collections.Generic;
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
        private string _trackedGameId = string.Empty;
        private string _trackedPlayerId = string.Empty;

        public LobbyConnectionRule(LobbyService lobbyService, List<ILobbyConnectionHandler> handlers)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _handlers = handlers ?? new List<ILobbyConnectionHandler>();
        }

        public void Initialize()
        {
            _lobbyService.State
                .Subscribe(OnLobbyStateChanged)
                .AddTo(_disposables);
        }

        private void OnLobbyStateChanged(LobbyState state)
        {
            switch (state.Status)
            {
                case LobbyStatus.Started:
                    if (ShouldConnect())
                    {
                        BeginConnect().Forget();
                    }
                    break;
                default:
                    if (_connected)
                    {
                        DisconnectAll().Forget();
                    }
                    break;
            }
        }

        private bool ShouldConnect()
        {
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
            if (!_lobbyService.TryGetSession(out var gameId, out var playerId))
            {
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
                    await handler.ConnectAsync(session, _cts.Token);
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

        public void Dispose()
        {
            _disposables.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
