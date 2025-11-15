using System.Collections.Generic;
using Modules.Players.Data;
using R3;

namespace Modules.Players.Interfaces
{
    public interface IPlayerRosterService
    {
        ReadOnlyReactiveProperty<IReadOnlyList<PlayerInfo>> Players { get; }

        bool TryGetPlayer(string playerId, out PlayerInfo info);

        void SetSnapshot(IEnumerable<PlayerInfo> players);

        void Upsert(PlayerInfo info);

        void Remove(string playerId);

        void Clear();

        void SetLocalPlayer(string playerId);
    }
}
