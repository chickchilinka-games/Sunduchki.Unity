using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using R3;
using UnityEngine;

namespace Modules.Lobby.Model
{
    public class LobbyStateContext : ILobbySignalRListener, IDisposable
    {
        private readonly LobbyModel _model;
        private readonly Subject<IReadOnlyList<LobbyPlayerInfo>> _readyToStart = new();
        private readonly Subject<LobbyGameStartedPayload> _gameStarted = new();
        private readonly Subject<LobbyGameEndedPayload> _gameEnded = new();
        private readonly Subject<LobbyChestUpdatedPayload> _chestUpdated = new();
        private readonly Dictionary<string, int> _chestCounts = new(StringComparer.Ordinal);

        private LobbyData _data;
        private bool _connected;

        public LobbyStateContext(LobbyModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _data = new LobbyData();
        }

        public ReadOnlyReactiveProperty<LobbyState> State => _model.State;
        public ReadOnlyReactiveProperty<IReadOnlyList<LobbyPlayerInfo>> Players => _model.Players;

        public Observable<IReadOnlyList<LobbyPlayerInfo>> ReadyToStart => _readyToStart;
        public Observable<LobbyGameStartedPayload> GameStarted => _gameStarted;
        public Observable<LobbyGameEndedPayload> GameEnded => _gameEnded;
        public Observable<LobbyChestUpdatedPayload> ChestUpdated => _chestUpdated;

        public LobbyData Data => _data;
        public bool IsConnected => _connected;

        public void UpdateConfig(LobbyData data)
        {
            _data = MergeConfig(_data, data);
            UpdateLocalPlayer();

            if (data.DeckCount.HasValue || data.TotalCards.HasValue)
            {
                _model.MutateState(state => state.With(builder =>
                {
                    builder.DeckCount = data.DeckCount ?? state.DeckCount;
                    builder.TotalCards = data.TotalCards ?? state.TotalCards;
                }));
            }

            if (data.IsHost.HasValue)
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
                var info = CreatePlayerInfo(player.Id, player.IsLocal);
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

        public LobbyPlayerInfo CreatePlayerInfo(string playerId, bool isLocal)
        {
            var localFlag = isLocal || IsLocal(playerId);
            return new LobbyPlayerInfo(playerId, localFlag);
        }

        public void ResetSession()
        {
            _connected = false;
            _data = new LobbyData();
            _model.ClearPlayers();
            _model.SetState(LobbyState.Default);
            _chestCounts.Clear();
        }

        public void EvaluateReadyToStart()
        {
            if (_data.IsHost != true)
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

        public void OnPlayerJoined(string playerId)
        {
            var info = CreatePlayerInfo(playerId, IsLocal(playerId));
            _model.UpsertPlayer(info);
            _model.MutateState(state => state.WithStatus(LobbyStatus.Waiting).ClearError());
            EvaluateReadyToStart();
        }

        public void OnPlayerLeft(string playerId)
        {
            _model.RemovePlayer(playerId);
            _model.MutateState(state => state.WithStatus(LobbyStatus.Waiting));
            EvaluateReadyToStart();

            var state = _model.CurrentState;
            if (!state.Started || state.Status == LobbyStatus.Ended)
            {
                return;
            }

            var localId = _data.PlayerId;
            if (string.IsNullOrWhiteSpace(localId))
            {
                var localPlayer = _model.CurrentPlayers?.FirstOrDefault(player => player.IsLocal);
                localId = localPlayer?.Id ?? string.Empty;
            }
            var isLocalLeft = IsLocal(playerId);
            var reason = isLocalLeft ? "You left the game." : "Opponent left the game.";
            var winners = new List<string>();
            if (!isLocalLeft)
            {
                if (!string.IsNullOrWhiteSpace(localId))
                {
                    winners.Add(localId);
                }
                else
                {
                    var localPlayer = _model.CurrentPlayers?.FirstOrDefault(player => player.IsLocal);
                    if (localPlayer.HasValue && !string.IsNullOrWhiteSpace(localPlayer.Value.Id))
                    {
                        winners.Add(localPlayer.Value.Id);
                    }
                    else if (_model.CurrentPlayers != null)
                    {
                        foreach (var player in _model.CurrentPlayers)
                        {
                            if (!string.IsNullOrWhiteSpace(player.Id))
                            {
                                winners.Add(player.Id);
                                break;
                            }
                        }
                    }
                }
            }

            Debug.Log($"[Lobby] OnPlayerLeft: playerId={playerId}, localId={localId}, isLocalLeft={isLocalLeft}, " +
                      $"started={state.Started}, status={state.Status}, winners=[{string.Join(",", winners)}], reason={reason}");

            ApplyGameEnded(new GameEndedResultDto(
                Array.Empty<GameEndedResultDto.PlayerChestResult>(),
                winners),
                reason);
        }

        public void OnGameStarted()
        {
            ResetChestCounts();
            _model.MutateState(state => state.WithStatus(LobbyStatus.Started).WithStarted(true));
            _gameStarted.OnNext(new LobbyGameStartedPayload(
                _data.GameId,
                _model.CurrentState.DeckCount,
                _model.CurrentState.TotalCards));
        }

        public void OnGameEnded(GameEndedResultDto payload)
        {
            if (payload == null)
            {
                return;
            }

            var state = _model.CurrentState;
            var hasWinners = payload.WinnerPlayerIds != null && payload.WinnerPlayerIds.Count > 0;
            var alreadyEnded = state.Status == LobbyStatus.Ended;
            var existingWinners = state.Result?.WinnerPlayerIds != null && state.Result.WinnerPlayerIds.Count > 0;
            var hasReason = !string.IsNullOrWhiteSpace(state.GameEndedReason);

            if (alreadyEnded && !hasWinners && (existingWinners || hasReason))
            {
                Debug.Log("[Lobby] Ignoring empty GameEnded payload because a reason/winner already exists.");
                return;
            }

            ApplyGameEnded(payload, null);
        }

        public void OnSetCompleted(string playerId, string rank)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var next = GetChestCount(playerId) + 1;
            _chestCounts[playerId] = next;
            _chestUpdated.OnNext(new LobbyChestUpdatedPayload(playerId, next, rank));
        }

        public void OnConnectionClosed(string error)
        {
            var state = _model.CurrentState;
            if (state.Status == LobbyStatus.Ended || !state.Started)
            {
                return;
            }

            var reason = string.IsNullOrWhiteSpace(error)
                ? "Connection lost. The game ended due to a server issue."
                : $"Connection lost: {error}";

            ApplyGameEnded(new GameEndedResultDto(
                Array.Empty<GameEndedResultDto.PlayerChestResult>(),
                Array.Empty<string>()),
                reason);
        }

        public void Dispose()
        {
            _readyToStart.Dispose();
            _gameStarted.Dispose();
            _gameEnded.Dispose();
            _chestUpdated.Dispose();
        }

        private void UpdateLocalPlayer()
        {
            if (string.IsNullOrWhiteSpace(_data.PlayerId))
            {
                return;
            }

            var localPlayer = CreatePlayerInfo(_data.PlayerId, true);
            _model.UpsertPlayer(localPlayer);
        }

        private bool IsLocal(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) &&
                   string.Equals(playerId, _data.PlayerId, StringComparison.Ordinal);
        }

        private int GetChestCount(string playerId)
        {
            return _chestCounts.TryGetValue(playerId, out var value) ? value : 0;
        }

        private void ResetChestCounts()
        {
            _chestCounts.Clear();
        }

        private void ApplyChestTable(GameEndedResultDto payload)
        {
            if (payload == null)
            {
                return;
            }

            _chestCounts.Clear();
            foreach (var entry in payload.Players)
            {
                var playerId = entry.PlayerId;
                var count = entry.ChestCount;
                _chestCounts[playerId] = count;
                _chestUpdated.OnNext(new LobbyChestUpdatedPayload(playerId, count, string.Empty));
            }
        }

        private void ApplyGameEnded(GameEndedResultDto payload, string reason)
        {
            _model.MutateState(state => state.WithStatus(LobbyStatus.Ended)
                .WithStarted(false)
                .With(builder =>
                {
                    builder.Result = payload;
                    builder.GameEndedReason = reason;
                }));

            ApplyChestTable(payload);
            _gameEnded.OnNext(new LobbyGameEndedPayload(payload, reason));
            Debug.Log($"[Lobby] GameEnded applied: winners=[{string.Join(",", payload.WinnerPlayerIds)}], reason={reason}");
        }

        private static LobbyData MergeConfig(in LobbyData source, in LobbyData incoming)
        {
            return new LobbyData
            {
                GameId = string.IsNullOrWhiteSpace(incoming.GameId) ? source.GameId : incoming.GameId,
                PlayerId = string.IsNullOrWhiteSpace(incoming.PlayerId) ? source.PlayerId : incoming.PlayerId,
                IsHost = incoming.IsHost ?? source.IsHost,
                DeckCount = incoming.DeckCount ?? source.DeckCount,
                TotalCards = incoming.TotalCards ?? source.TotalCards
            };
        }
    }
}
