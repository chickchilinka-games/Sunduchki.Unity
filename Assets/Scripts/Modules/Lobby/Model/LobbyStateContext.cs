using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using R3;

namespace Modules.Lobby.Model
{
    public class LobbyStateContext : ILobbySignalRListener, IDisposable
    {
        private readonly LobbyModel _model;
        private readonly Subject<IReadOnlyList<LobbyPlayerInfo>> _readyToStart = new();
        private readonly Subject<LobbyGameStartedPayload> _gameStarted = new();
        private readonly Subject<LobbyGameEndedPayload> _gameEnded = new();

        private LobbyConfig _config;
        private bool _connected;

        public LobbyStateContext(LobbyModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _config = new LobbyConfig();
        }

        public ReadOnlyReactiveProperty<LobbyState> State => _model.State;
        public ReadOnlyReactiveProperty<IReadOnlyList<LobbyPlayerInfo>> Players => _model.Players;

        public Observable<IReadOnlyList<LobbyPlayerInfo>> ReadyToStart => _readyToStart;
        public Observable<LobbyGameStartedPayload> GameStarted => _gameStarted;
        public Observable<LobbyGameEndedPayload> GameEnded => _gameEnded;

        public LobbyConfig Config => _config;
        public bool IsConnected => _connected;

        public void UpdateConfig(LobbyConfig config)
        {
            _config = MergeConfig(_config, config);
            UpdateLocalPlayer();

            if (config.DeckCount.HasValue || config.TotalCards.HasValue)
            {
                _model.MutateState(state => state.With(builder =>
                {
                    builder.DeckCount = config.DeckCount ?? state.DeckCount;
                    builder.TotalCards = config.TotalCards ?? state.TotalCards;
                }));
            }

            if (config.IsHost.HasValue)
            {
                EvaluateReadyToStart();
            }
        }

        public void SetPlayers(IEnumerable<LobbyPlayerInfo> players)
        {
            _model.SetPlayers(players);
            EvaluateReadyToStart();
        }

        public void ApplyInitialPlayers(IEnumerable<LobbyPlayerInfo> players)
        {
            if (players == null)
            {
                return;
            }

            foreach (var player in players)
            {
                var info = CreatePlayerInfo(player.Id, player.Name, player.IsLocal);
                _model.UpsertPlayer(info);
            }

            EvaluateReadyToStart();
        }

        public void MutateState(Func<LobbyState, LobbyState> updater)
        {
            _model.MutateState(updater);
        }

        public void SetConnected(bool connected)
        {
            _connected = connected;
        }

        public LobbyPlayerInfo CreatePlayerInfo(string playerId, string name, bool isLocal)
        {
            var resolvedName = ResolveName(playerId, name);
            var localFlag = isLocal || IsLocal(playerId);
            return new LobbyPlayerInfo(playerId, resolvedName, localFlag);
        }

        public void ResetSession()
        {
            _connected = false;
            _config = new LobbyConfig();
            _model.ClearPlayers();
            _model.SetState(LobbyState.Default);
        }

        public void EvaluateReadyToStart()
        {
            if (_config.IsHost != true)
            {
                return;
            }

            var players = _model.CurrentPlayers;
            if (players == null)
            {
                return;
            }

            if (players.Count >= 2 && _model.CurrentState.Status != LobbyStatus.Starting)
            {
                _readyToStart.OnNext(players);
            }
        }

        public void OnPlayerJoined(string playerId, string playerName)
        {
            var info = CreatePlayerInfo(playerId, playerName, IsLocal(playerId));
            _model.UpsertPlayer(info);
            _model.MutateState(state => state.WithStatus(LobbyStatus.Waiting).ClearError());
            EvaluateReadyToStart();
        }

        public void OnPlayerLeft(string playerId)
        {
            _model.RemovePlayer(playerId);
            _model.MutateState(state => state.WithStatus(LobbyStatus.Waiting));
            EvaluateReadyToStart();
        }

        public void OnGameStarted()
        {
            _model.MutateState(state => state.WithStatus(LobbyStatus.Started).WithStarted(true));
            _gameStarted.OnNext(new LobbyGameStartedPayload(
                _config.GameId,
                _model.CurrentState.DeckCount,
                _model.CurrentState.TotalCards));
        }

        public void OnGameEnded(object payload)
        {
            _model.MutateState(state => state.WithStatus(LobbyStatus.Ended)
                .WithStarted(false)
                .With(builder =>
                {
                    builder.Result = payload;
                }));

            _gameEnded.OnNext(new LobbyGameEndedPayload(payload));
        }

        public void Dispose()
        {
            _readyToStart.Dispose();
            _gameStarted.Dispose();
            _gameEnded.Dispose();
        }

        private void UpdateLocalPlayer()
        {
            if (string.IsNullOrWhiteSpace(_config.PlayerId))
            {
                return;
            }

            var localPlayer = CreatePlayerInfo(_config.PlayerId, _config.PlayerName, true);
            _model.UpsertPlayer(localPlayer);
        }

        private bool IsLocal(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) &&
                   string.Equals(playerId, _config.PlayerId, StringComparison.Ordinal);
        }

        private static LobbyConfig MergeConfig(in LobbyConfig source, in LobbyConfig incoming)
        {
            return new LobbyConfig
            {
                GameId = string.IsNullOrWhiteSpace(incoming.GameId) ? source.GameId : incoming.GameId,
                PlayerId = string.IsNullOrWhiteSpace(incoming.PlayerId) ? source.PlayerId : incoming.PlayerId,
                PlayerName = string.IsNullOrWhiteSpace(incoming.PlayerName) ? source.PlayerName : incoming.PlayerName,
                IsHost = incoming.IsHost ?? source.IsHost,
                DeckCount = incoming.DeckCount ?? source.DeckCount,
                TotalCards = incoming.TotalCards ?? source.TotalCards
            };
        }

        private static string ResolveName(string playerId, string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return "Player";
            }

            var suffix = playerId.Length > 4 ? playerId[..4] : playerId;
            return $"Player {suffix}";
        }
    }
}
