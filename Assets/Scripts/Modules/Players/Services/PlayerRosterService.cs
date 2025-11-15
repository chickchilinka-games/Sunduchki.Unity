using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Players.Data;
using Modules.Players.Interfaces;
using Modules.Players.Model;
using R3;

namespace Modules.Players.Services
{
    public class PlayerRosterService : IPlayerRosterService
    {
        private readonly PlayerRosterModel _model;
        private readonly Dictionary<string, PlayerInfo> _playersById = new();
        private string _localPlayerId;

        public PlayerRosterService(PlayerRosterModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ReadOnlyReactiveProperty<IReadOnlyList<PlayerInfo>> Players => _model.Players;

        public bool TryGetPlayer(string playerId, out PlayerInfo info)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                info = default;
                return false;
            }

            return _playersById.TryGetValue(playerId, out info);
        }

        public void SetSnapshot(IEnumerable<PlayerInfo> players)
        {
            _playersById.Clear();
            if (players != null)
            {
                foreach (var player in players)
                {
                    var normalized = ApplyLocalFlag(player);
                    _playersById[normalized.Id] = normalized;
                }
            }

            Publish();
        }

        public void Upsert(PlayerInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Id))
            {
                return;
            }

            _playersById[info.Id] = ApplyLocalFlag(info);
            Publish();
        }

        public void Remove(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            if (_playersById.Remove(playerId))
            {
                Publish();
            }
        }

        public void Clear()
        {
            _playersById.Clear();
            Publish();
        }

        public void SetLocalPlayer(string playerId)
        {
            _localPlayerId = playerId ?? string.Empty;
            Publish();
        }

        private PlayerInfo ApplyLocalFlag(PlayerInfo info)
        {
            var isLocal = !string.IsNullOrWhiteSpace(_localPlayerId) &&
                          string.Equals(info.Id, _localPlayerId, StringComparison.Ordinal);
            return info.WithLocal(isLocal);
        }

        private void Publish()
        {
            _model.SetPlayers(_playersById.Values.ToArray());
        }
    }
}
