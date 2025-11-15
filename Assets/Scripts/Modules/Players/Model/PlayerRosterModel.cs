using System.Collections.Generic;
using Modules.Players.Data;
using R3;

namespace Modules.Players.Model
{
    public class PlayerRosterModel
    {
        private readonly ReactiveProperty<IReadOnlyList<PlayerInfo>> _players =
            new(System.Array.Empty<PlayerInfo>());

        public ReadOnlyReactiveProperty<IReadOnlyList<PlayerInfo>> Players => _players;

        public void SetPlayers(IReadOnlyList<PlayerInfo> players)
        {
            _players.Value = players ?? System.Array.Empty<PlayerInfo>();
        }
    }
}
