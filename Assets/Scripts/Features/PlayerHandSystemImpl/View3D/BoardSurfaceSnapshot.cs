using System;
using System.Collections.Generic;

namespace Features.PlayerHandSystemImpl.View3D
{
    public readonly struct BoardRankSetSnapshot
    {
        public BoardRankSetSnapshot(string rank, IReadOnlyList<string> suits)
        {
            Rank = rank ?? string.Empty;
            Suits = suits ?? Array.Empty<string>();
        }

        public string Rank { get; }
        public IReadOnlyList<string> Suits { get; }
    }

    public readonly struct BoardBonusSetSnapshot
    {
        public BoardBonusSetSnapshot(string bonusType, int count)
        {
            BonusType = bonusType ?? string.Empty;
            Count = count;
        }

        public string BonusType { get; }
        public int Count { get; }
    }

    public readonly struct BoardSurfaceSnapshot
    {
        public static readonly BoardSurfaceSnapshot Empty =
            new(Array.Empty<BoardRankSetSnapshot>(), Array.Empty<BoardBonusSetSnapshot>());

        public BoardSurfaceSnapshot(
            IReadOnlyList<BoardRankSetSnapshot> rankSets,
            IReadOnlyList<BoardBonusSetSnapshot> bonusSets)
        {
            RankSets = rankSets ?? Array.Empty<BoardRankSetSnapshot>();
            BonusSets = bonusSets ?? Array.Empty<BoardBonusSetSnapshot>();
        }

        public IReadOnlyList<BoardRankSetSnapshot> RankSets { get; }
        public IReadOnlyList<BoardBonusSetSnapshot> BonusSets { get; }
    }
}
