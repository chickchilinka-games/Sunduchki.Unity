using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using UnityEngine;

namespace Modules.Lobby.Providers
{
    internal class SignalRLobbyClient : ILobbySignalRClient
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private readonly List<IDisposable> _subscriptions = new();
        private ILobbySignalRListener _listener;
        private bool _joined;
        private bool _startedSignaled;
        private string _joinedGameId = string.Empty;
        private string _joinedPlayerId = string.Empty;

        public SignalRLobbyClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public async UniTask ConnectAsync(
            LobbySignalRConnectionOptions options,
            ILobbySignalRListener listener,
            CancellationToken cancellationToken = default)
        {
            if (options.HubUri == null)
            {
                throw new ArgumentException("Hub URI must be provided.", nameof(options));
            }

            await DisconnectAsync(cancellationToken);

            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            RegisterHandlers(_listener);
            Debug.Log($"[Lobby] SignalR shared transport is ready. Hub={options.HubUri}");
        }

        public async UniTask JoinGameAsync(LobbySignalRJoinPayload payload, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            Debug.Log($"[Lobby] JoinGame invoke: gameId={payload.GameId}, playerId={payload.PlayerId}");
            await _sharedHubConnection.AcquireAsync(
                payload.GameId,
                payload.PlayerId,
                cancellationToken,
                forceJoin: true);
            _joined = true;
            _joinedGameId = payload.GameId ?? string.Empty;
            _joinedPlayerId = payload.PlayerId ?? string.Empty;
        }

        public async UniTask LeaveGameAsync(LobbySignalRLeavePayload payload, CancellationToken cancellationToken = default)
        {
            EnsureConnected();
            await _sharedHubConnection.InvokeAsync("LeaveGame", new LeaveGameRequestDto
            {
                GameId = payload.GameId,
                PlayerId = payload.PlayerId
            }, cancellationToken);
        }

        public async UniTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            ClearSubscriptions();

            if (_joined)
            {
                _joined = false;
                await _sharedHubConnection.ReleaseAsync();
            }

            _listener = null;
            _startedSignaled = false;
            _joinedGameId = string.Empty;
            _joinedPlayerId = string.Empty;
        }

        private void RegisterHandlers(ILobbySignalRListener listener)
        {
            ClearSubscriptions();

            _subscriptions.Add(_sharedHubConnection.SubscribeClosed(error =>
            {
                listener.OnConnectionClosed(error);
            }));

            _subscriptions.Add(_sharedHubConnection.SubscribeReconnected(() =>
            {
                Debug.Log("[Lobby] SignalR reconnected and rejoined.");
                RequestSyncState().Forget();
            }));

            _subscriptions.Add(_sharedHubConnection.Subscribe<PlayerJoinedDto>("PlayerJoined", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                Debug.Log($"[Lobby] PlayerJoined received: {payload.PlayerId}");
                listener.OnPlayerJoined(payload.PlayerId);
            }));

            _subscriptions.Add(_sharedHubConnection.Subscribe<string>("PlayerLeft", playerId =>
            {
                Debug.Log($"[Lobby] PlayerLeft received: {playerId}");
                listener.OnPlayerLeft(playerId);
            }));

            _subscriptions.Add(_sharedHubConnection.Subscribe("GameStarted", () =>
            {
                NotifyGameStarted(listener, "GameStarted");
            }));

            _subscriptions.Add(_sharedHubConnection.Subscribe<string>("TurnAdvanced", _ =>
            {
                // WebGL no-arg events can be dropped in edge races; turn advance is a reliable
                // in-progress signal sent on join for started games.
                NotifyGameStarted(listener, "TurnAdvanced");
            }));

            _subscriptions.Add(_sharedHubConnection.Subscribe<GameEndedResultDto>("GameEnded", payload =>
            {
                Debug.Log($"[Lobby] GameEnded received: winners={payload?.WinnerPlayerIds?.Count ?? 0}");
                listener.OnGameEnded(payload ?? new GameEndedResultDto(
                    Array.Empty<GameEndedResultDto.PlayerChestResult>(),
                    Array.Empty<string>()));
            }));

            _subscriptions.Add(_sharedHubConnection.Subscribe<HandDeltaDto>("HandDelta", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                if (!string.Equals(payload.Destination, "chest", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!payload.CompletedSet)
                {
                    return;
                }

                var rank = payload.CompletedSetRank;
                if (string.IsNullOrWhiteSpace(rank) &&
                    payload.RemovedCards != null &&
                    payload.RemovedCards.Length > 0)
                {
                    rank = payload.RemovedCards[0]?.Rank ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(rank))
                {
                    return;
                }

                Debug.Log($"[Lobby] Chest completed via transfer: playerId={payload.PlayerId}, rank={rank}");
                listener.OnSetCompleted(payload.PlayerId, rank);
            }));
        }

        private void ClearSubscriptions()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }

            _subscriptions.Clear();
        }

        private void EnsureConnected()
        {
            if (_listener == null)
            {
                throw new InvalidOperationException("SignalR connection has not been established. Call ConnectAsync first.");
            }
        }

        private async UniTaskVoid RequestSyncState()
        {
            if (!_joined)
            {
                return;
            }

            var gameId = _joinedGameId;
            var playerId = _joinedPlayerId;

            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            try
            {
                await _sharedHubConnection.InvokeAsync("SyncState", new JoinGameRequestDto
                {
                    GameId = gameId,
                    PlayerId = playerId
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Lobby] SyncState after reconnect failed: {ex.Message}");
            }
        }

        private void NotifyGameStarted(ILobbySignalRListener listener, string source)
        {
            if (_startedSignaled)
            {
                return;
            }

            _startedSignaled = true;
            Debug.Log($"[Lobby] GameStarted inferred from {source}.");
            listener.OnGameStarted();
            RequestSyncState().Forget();
        }

        private sealed class PlayerJoinedDto
        {
            public string PlayerId { get; set; }
        }

        private sealed class HandDeltaDto
        {
            public string PlayerId { get; set; }
            public string Destination { get; set; }
            public TransferCardDto[] RemovedCards { get; set; }
            public bool CompletedSet { get; set; }
            public string CompletedSetRank { get; set; }
        }

        private sealed class TransferCardDto
        {
            public string Rank { get; set; }
            public string Suit { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class LeaveGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }
    }
}
