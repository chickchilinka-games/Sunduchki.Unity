using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.Lobby.Model;
using R3;
using UnityEngine;

namespace Modules.Lobby.Services
{
    public class LobbyService
    {
        private readonly ILobbyApiClient _apiClient;
        private readonly ILobbySignalRClient _signalRClient;
        private readonly ITokenProvider _tokenProvider;
        private readonly LobbyStateContext _state;
        private readonly IReadOnlyList<ILobbyConnectionHandler> _connectionHandlers;
        private bool _handlersConnected;
        private string _connectedGameId = string.Empty;
        private string _connectedPlayerId = string.Empty;

        internal LobbyService(
            ILobbyApiClient apiClient,
            ILobbySignalRClient signalRClient,
            ITokenProvider tokenProvider,
            LobbyStateContext state,
            List<ILobbyConnectionHandler> connectionHandlers)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _signalRClient = signalRClient ?? throw new ArgumentNullException(nameof(signalRClient));
            _tokenProvider = tokenProvider;
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _connectionHandlers = (connectionHandlers ?? new List<ILobbyConnectionHandler>())
                .OrderBy(GetHandlerPriority)
                .ThenBy(handler => handler?.GetType().FullName, StringComparer.Ordinal)
                .ToArray();
        }

        public ReadOnlyReactiveProperty<LobbyState> State => _state.State;
        public ReadOnlyReactiveProperty<IReadOnlyList<LobbyPlayerInfo>> Players => _state.Players;
        public Observable<IReadOnlyList<LobbyPlayerInfo>> ReadyToStart => _state.ReadyToStart;
        public Observable<LobbyGameStartedPayload> GameStarted => _state.GameStarted;
        public Observable<LobbyGameEndedPayload> GameEnded => _state.GameEnded;
        public Observable<LobbyChestUpdatedPayload> ChestUpdated => _state.ChestUpdated;
        public bool IsConnected => _state.IsConnected;


        public LobbyPlayerInfo GetLocalPlayer()
        {
            return _state.Players.CurrentValue.FirstOrDefault(player=>player.IsLocal);    
        }

        public string GetGameId()
        {
            return _state.Data.GameId ?? string.Empty;
        }

        public string GetLocalPlayerId()
        {
            return _state.Data.PlayerId ?? string.Empty;
        }

        public bool TryGetGameId(out string gameId)
        {
            gameId = _state.Data.GameId ?? string.Empty;
            return !string.IsNullOrWhiteSpace(gameId);
        }

        public bool TryGetLocalPlayerId(out string playerId)
        {
            playerId = _state.Data.PlayerId ?? string.Empty;
            return !string.IsNullOrWhiteSpace(playerId);
        }

        public bool TryGetDeckConfig(out int? deckCount, out int? totalCards)
        {
            deckCount = _state.Data.DeckCount;
            totalCards = _state.Data.TotalCards;
            return deckCount.HasValue || totalCards.HasValue;
        }

        public bool TryGetSession(out string gameId, out string playerId)
        {
            var data = _state.Data;
            gameId = data.GameId ?? string.Empty;
            playerId = data.PlayerId ?? string.Empty;
            return !string.IsNullOrWhiteSpace(gameId) && !string.IsNullOrWhiteSpace(playerId);
        }
        
        public void UpdateData(LobbyData data)
        {
            _state.UpdateConfig(data);
        }

        public void ApplyInitialPlayers(IEnumerable<LobbyPlayerInfo> players)
        {
            _state.ApplyInitialPlayers(players);
        }

        public async UniTask<CreateGameResult> CreateGameAsync(CreateGameOptions options, CancellationToken cancellationToken = default)
        {
            _state.MutateState(state => state.WithStatus(LobbyStatus.Creating).ClearError());
            try
            {
                var result = await _apiClient.CreateGameAsync(options, cancellationToken);
                _state.MutateState(state => state.WithStatus(LobbyStatus.Idle).ClearError());
                return result;
            }
            catch (Exception ex)
            {
                HandleError("create-game", ex);
                throw;
            }
        }

        public async UniTask<JoinGameResult> JoinGameAsync(string gameId, JoinGameOptions options, CancellationToken cancellationToken = default)
        {
            _state.MutateState(state => state.WithStatus(LobbyStatus.Joining).ClearError());
            try
            {
                var result = await _apiClient.JoinGameAsync(gameId, options, cancellationToken);
                ApplyMatchData(gameId, result.PlayerId, result.Players, result.DeckCount, result.TotalCards, result.Started);
                _state.MutateState(state => state.WithStatus(LobbyStatus.Idle).ClearError());
                return result;
            }
            catch (Exception ex)
            {
                HandleError("join-game", ex);
                throw;
            }
        }

        public async UniTask<MatchmakingResult> SearchMatchAsync(MatchmakingOptions options, CancellationToken cancellationToken = default)
        {
            _state.MutateState(state => state.WithStatus(LobbyStatus.Joining).ClearError());
            try
            {
                var result = await _apiClient.SearchMatchAsync(options, cancellationToken);
                ApplyMatchData(result.GameId, result.PlayerId, result.Players, result.DeckCount, result.TotalCards, result.Started);
                _state.MutateState(state => state.WithStatus(LobbyStatus.Idle).ClearError());
                return result;
            }
            catch (Exception ex)
            {
                HandleError("search-match", ex);
                throw;
            }
        }

        public async UniTask ConnectAsync(Uri hubUri, CancellationToken cancellationToken = default)
        {
            if (hubUri == null)
            {
                throw new ArgumentException("Hub URI must be provided.", nameof(hubUri));
            }

            var accessToken = _tokenProvider?.GetToken() ?? string.Empty;
            var options = new LobbySignalRConnectionOptions(hubUri, accessToken);

            EnsureConfigured();

            if (_state.IsConnected)
            {
                if (IsConnectedToCurrentSession())
                {
                    return;
                }

                Debug.LogWarning(
                    $"[Lobby] Session changed while connected. Reconnecting transport. " +
                    $"oldGame={_connectedGameId}, newGame={_state.Data.GameId}, " +
                    $"oldPlayer={_connectedPlayerId}, newPlayer={_state.Data.PlayerId}");
                await DisconnectTransportAsync(cancellationToken, resetState: false);
            }

            _state.MutateState(state => state.WithStatus(LobbyStatus.Connecting).ClearError());
            try
            {
                await _signalRClient.ConnectAsync(options, _state, cancellationToken);
                await ConnectModuleHandlersAsync(cancellationToken);
                await _signalRClient.JoinGameAsync(
                    new LobbySignalRJoinPayload(_state.Data.GameId, _state.Data.PlayerId),
                    cancellationToken);

                _state.SetConnected(true);
                _connectedGameId = _state.Data.GameId ?? string.Empty;
                _connectedPlayerId = _state.Data.PlayerId ?? string.Empty;
                _state.MutateState(state =>
                {
                    if (state.Status == LobbyStatus.Ended)
                    {
                        return state;
                    }

                    if (state.Started)
                    {
                        return state.WithStatus(LobbyStatus.Started).ClearError();
                    }

                    return state.WithStatus(LobbyStatus.Waiting).ClearError();
                });
                _state.EvaluateReadyToStart();
            }
            catch (Exception ex)
            {
                await DisconnectTransportAsync(cancellationToken, resetState: false);

                HandleError("connect", ex, LobbyStatus.Idle);
                throw;
            }
        }

        public async UniTask StartGameAsync(CancellationToken cancellationToken = default)
        {
            if (_state.Data.IsHost != true)
            {
                Debug.LogWarning("[Lobby] StartGameAsync called by non-host client.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_state.Data.GameId))
            {
                Debug.LogWarning("[Lobby] StartGameAsync requires configured GameId.");
                return;
            }

            _state.MutateState(state => state.WithStatus(LobbyStatus.Starting).ClearError());
            try
            {
                await _apiClient.StartGameAsync(_state.Data.GameId, cancellationToken);
            }
            catch (Exception ex)
            {
                HandleError("start-game", ex, LobbyStatus.Waiting);
                throw;
            }
        }

        public async UniTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_state.IsConnected &&
                !_handlersConnected &&
                string.IsNullOrWhiteSpace(_connectedGameId) &&
                string.IsNullOrWhiteSpace(_connectedPlayerId))
            {
                return;
            }

            await DisconnectTransportAsync(cancellationToken, resetState: true);
        }

        private async UniTask DisconnectTransportAsync(CancellationToken cancellationToken, bool resetState)
        {
            try
            {
                await LeaveConnectedSessionAsync(cancellationToken);
                await DisconnectModuleHandlersAsync();
                await _signalRClient.DisconnectAsync(cancellationToken);
            }
            finally
            {
                _state.SetConnected(false);
                _connectedGameId = string.Empty;
                _connectedPlayerId = string.Empty;

                if (resetState)
                {
                    _state.MutateState(state => state.WithStatus(LobbyStatus.Idle)
                        .WithStarted(false)
                        .With(builder =>
                        {
                            builder.Result = null;
                            builder.GameEndedReason = null;
                        }));
                }
            }
        }

        private async UniTask LeaveConnectedSessionAsync(CancellationToken cancellationToken)
        {
            var gameId = string.IsNullOrWhiteSpace(_connectedGameId)
                ? _state.Data.GameId
                : _connectedGameId;
            var playerId = string.IsNullOrWhiteSpace(_connectedPlayerId)
                ? _state.Data.PlayerId
                : _connectedPlayerId;
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            try
            {
                await _signalRClient.LeaveGameAsync(
                    new LobbySignalRLeavePayload(gameId, playerId),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Lobby] LeaveGame failed: {ex.Message}");
            }
        }

        private bool IsConnectedToCurrentSession()
        {
            return _state.IsConnected &&
                   string.Equals(_connectedGameId, _state.Data.GameId, StringComparison.Ordinal) &&
                   string.Equals(_connectedPlayerId, _state.Data.PlayerId, StringComparison.Ordinal);
        }

        public void SetHost(bool isHost)
        {
            UpdateData(new LobbyData { IsHost = isHost });
        }

        private void HandleError(string context, Exception exception, LobbyStatus fallbackStatus = LobbyStatus.Idle)
        {
            var message = exception?.Message ?? "Unknown error";
            Debug.LogError($"[Lobby] Failed to {context}: {message}");
            if(exception!=null)
                Debug.LogException(exception);

            _state.MutateState(state => state.WithStatus(fallbackStatus).WithError(message));
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_state.Data.GameId) || string.IsNullOrWhiteSpace(_state.Data.PlayerId))
            {
                throw new InvalidOperationException("Lobby configuration is incomplete. GameId and PlayerId are required.");
            }
        }

        private void ApplyMatchData(
            string gameId,
            string playerId,
            IReadOnlyList<LobbyPlayerInfo> players,
            int deckCount,
            int totalCards,
            bool started)
        {
            UpdateData(new LobbyData
            {
                GameId = gameId,
                PlayerId = playerId,
                DeckCount = deckCount,
                TotalCards = totalCards,
                IsHost = false
            });

            var roster = new List<LobbyPlayerInfo>
            {
                _state.CreatePlayerInfo(playerId, true)
            };

            if (players != null)
            {
                foreach (var player in players)
                {
                    roster.Add(_state.CreatePlayerInfo(player.Id, false));
                }
            }

            _state.SetPlayers(roster);
            _state.MutateState(state =>
            {
                var next = state.WithStarted(started);
                if (started)
                {
                    next = next.WithStatus(LobbyStatus.Started);
                }

                return next;
            });
        }

        private async UniTask ConnectModuleHandlersAsync(CancellationToken cancellationToken)
        {
            if (_handlersConnected)
            {
                return;
            }

            if (!TryGetSession(out var gameId, out var playerId))
            {
                Debug.LogWarning("[Lobby] Cannot connect module handlers: session is not configured.");
                return;
            }

            var session = new LobbySession(gameId, playerId);
            var connectedHandlers = new List<ILobbyConnectionHandler>(_connectionHandlers.Count);
            try
            {
                foreach (var handler in _connectionHandlers)
                {
                    var handlerName = handler.GetType().Name;
                    Debug.Log($"[Lobby] Connecting handler: {handlerName}");
                    await handler.ConnectAsync(session, cancellationToken);
                    connectedHandlers.Add(handler);
                    Debug.Log($"[Lobby] Connected handler: {handlerName}");
                }

                _handlersConnected = true;
            }
            catch
            {
                for (var i = connectedHandlers.Count - 1; i >= 0; i--)
                {
                    await connectedHandlers[i].DisconnectAsync();
                }

                throw;
            }
        }

        private async UniTask DisconnectModuleHandlersAsync()
        {
            if (!_handlersConnected)
            {
                return;
            }

            for (var i = _connectionHandlers.Count - 1; i >= 0; i--)
            {
                await _connectionHandlers[i].DisconnectAsync();
            }

            _handlersConnected = false;
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
    }
}
