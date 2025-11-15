using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Lobby.Data;
using R3;

namespace Modules.Lobby.Model
{
    public class LobbyModel
    {
        private readonly ReactiveProperty<LobbyState> _state = new(LobbyState.Default);
        private readonly ReactiveProperty<IReadOnlyList<LobbyPlayerInfo>> _players =
            new(Array.Empty<LobbyPlayerInfo>());

        public ReadOnlyReactiveProperty<LobbyState> State => _state;
        public ReadOnlyReactiveProperty<IReadOnlyList<LobbyPlayerInfo>> Players => _players;

        public LobbyState CurrentState => _state.Value;
        public IReadOnlyList<LobbyPlayerInfo> CurrentPlayers => _players.Value;

        public void SetState(LobbyState state)
        {
            _state.Value = state;
        }

        public void MutateState(Func<LobbyState, LobbyState> updater)
        {
            _state.Value = updater(_state.Value);
        }

        public void SetPlayers(IEnumerable<LobbyPlayerInfo> players)
        {
            _players.Value = players?.ToArray() ?? Array.Empty<LobbyPlayerInfo>();
        }

        public void UpsertPlayer(LobbyPlayerInfo player)
        {
            var list = _players.Value.ToList();
            var existingIndex = list.FindIndex(info => info.Id == player.Id);
            if (existingIndex >= 0)
            {
                list[existingIndex] = player;
            }
            else
            {
                list.Add(player);
            }

            _players.Value = list;
        }

        public void RemovePlayer(string playerId)
        {
            var list = _players.Value.ToList();
            var removed = list.RemoveAll(info => info.Id == playerId);
            if (removed > 0)
            {
                _players.Value = list;
            }
        }

        public void ClearPlayers()
        {
            _players.Value = Array.Empty<LobbyPlayerInfo>();
        }
    }
}
