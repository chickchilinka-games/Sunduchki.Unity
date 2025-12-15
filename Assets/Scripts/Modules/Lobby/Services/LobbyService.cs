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
        private readonly LobbyStateContext _state;

        public LobbyService(
            ILobbyApiClient apiClient,
            ILobbySignalRClient signalRClient,
            LobbyStateContext state)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _signalRClient = signalRClient ?? throw new ArgumentNullException(nameof(signalRClient));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public LobbyStateContext StateContext => _state;

        public ReadOnlyReactiveProperty<LobbyState> State => _state.State;
        public ReadOnlyReactiveProperty<IReadOnlyList<LobbyPlayerInfo>> Players => _state.Players;
        public Observable<IReadOnlyList<LobbyPlayerInfo>> ReadyToStart => _state.ReadyToStart;
        public Observable<LobbyGameStartedPayload> GameStarted => _state.GameStarted;
        public Observable<LobbyGameEndedPayload> GameEnded => _state.GameEnded;


        public LobbyPlayerInfo GetLocalPlayer()
        {
            return _state.Players.CurrentValue.FirstOrDefault(player=>player.IsLocal);    
        }
        
        public void UpdateConfig(LobbyConfig config)
        {
            _state.UpdateConfig(config);
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
                UpdateConfig(new LobbyConfig
                {
                    GameId = gameId,
                    PlayerId = result.PlayerId,
                    PlayerName = result.PlayerName,
                    DeckCount = result.DeckCount,
                    TotalCards = result.TotalCards
                });

                var roster = new List<LobbyPlayerInfo>
                {
                    _state.CreatePlayerInfo(result.PlayerId, result.PlayerName, true)
                };

                if (result.Players != null)
                {
                    foreach (var player in result.Players)
                    {
                        roster.Add(_state.CreatePlayerInfo(player.Id, player.Name, false));
                    }
                }

                _state.SetPlayers(roster);

                _state.MutateState(state => state.WithStatus(LobbyStatus.Idle).ClearError());
                return result;
            }
            catch (Exception ex)
            {
                HandleError("join-game", ex);
                throw;
            }
        }

        public async UniTask ConnectAsync(LobbySignalRConnectionOptions options, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            if (_state.IsConnected)
            {
                return;
            }

            _state.MutateState(state => state.WithStatus(LobbyStatus.Connecting).ClearError());
            try
            {
                await _signalRClient.ConnectAsync(options, _state, cancellationToken);
                await _signalRClient.JoinGameAsync(
                    new LobbySignalRJoinPayload(_state.Config.GameId, _state.Config.PlayerId),
                    cancellationToken);

                _state.SetConnected(true);
                _state.MutateState(state => state.WithStatus(LobbyStatus.Waiting).ClearError());
                _state.EvaluateReadyToStart();
            }
            catch (Exception ex)
            {
                HandleError("connect", ex, LobbyStatus.Idle);
                throw;
            }
        }

        public async UniTask StartGameAsync(CancellationToken cancellationToken = default)
        {
            if (_state.Config.IsHost != true)
            {
                Debug.LogWarning("[Lobby] StartGameAsync called by non-host client.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_state.Config.GameId))
            {
                Debug.LogWarning("[Lobby] StartGameAsync requires configured GameId.");
                return;
            }

            _state.MutateState(state => state.WithStatus(LobbyStatus.Starting).ClearError());
            try
            {
                await _apiClient.StartGameAsync(_state.Config.GameId, cancellationToken);
            }
            catch (Exception ex)
            {
                HandleError("start-game", ex, LobbyStatus.Waiting);
                throw;
            }
        }

        public async UniTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_state.IsConnected)
            {
                return;
            }

            try
            {
                await _signalRClient.DisconnectAsync(cancellationToken);
            }
            finally
            {
                _state.SetConnected(false);
                _state.MutateState(state => state.WithStatus(LobbyStatus.Idle)
                    .WithStarted(false)
                    .With(builder =>
                    {
                        builder.Result = null;
                    }));
            }
        }

        public void SetHost(bool isHost)
        {
            UpdateConfig(new LobbyConfig { IsHost = isHost });
        }

        private void HandleError(string context, Exception exception, LobbyStatus fallbackStatus = LobbyStatus.Idle)
        {
            var message = exception?.Message ?? "Unknown error";
            Debug.LogError($"[Lobby] Failed to {context}: {message}");

            _state.MutateState(state => state.WithStatus(fallbackStatus).WithError(message));
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_state.Config.GameId) || string.IsNullOrWhiteSpace(_state.Config.PlayerId))
            {
                throw new InvalidOperationException("Lobby configuration is incomplete. GameId and PlayerId are required.");
            }
        }
    }
}
