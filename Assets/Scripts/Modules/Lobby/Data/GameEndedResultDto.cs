using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.Lobby.Data
{
    public sealed class GameEndedResultDto
    {
        public IReadOnlyList<PlayerChestResult> Players { get; }
        public IReadOnlyList<string> WinnerPlayerIds { get; }

        public GameEndedResultDto(IReadOnlyList<PlayerChestResult> players, IReadOnlyList<string> winnerPlayerIds)
        {
            Players = players ?? Array.Empty<PlayerChestResult>();
            WinnerPlayerIds = winnerPlayerIds ?? Array.Empty<string>();
        }

        public bool TryGetChestCount(string playerId, out int count)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                count = 0;
                return false;
            }

            for (var i = 0; i < Players.Count; i++)
            {
                var entry = Players[i];
                if (string.Equals(entry.PlayerId, playerId, StringComparison.Ordinal))
                {
                    count = entry.ChestCount;
                    return true;
                }
            }

            count = 0;
            return false;
        }

        public int MaxChestCount()
        {
            return Players.Count == 0 ? 0 : Players.Max(entry => entry.ChestCount);
        }

        public sealed class PlayerChestResult
        {
            public string PlayerId { get; }
            public int ChestCount { get; }

            public PlayerChestResult(string playerId, int chestCount)
            {
                PlayerId = playerId ?? string.Empty;
                ChestCount = chestCount < 0 ? 0 : chestCount;
            }
        }
    }
}
