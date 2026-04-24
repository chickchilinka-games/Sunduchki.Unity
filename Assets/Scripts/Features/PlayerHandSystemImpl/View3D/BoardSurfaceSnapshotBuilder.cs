using System;
using System.Collections.Generic;
using Features.PlayerHandSystemImpl.Presentation.Presenters;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View3D
{
    public sealed class BoardSurfaceSnapshotBuilder
    {
        public BoardSurfaceSnapshot Build(
            PlayerHandPresenter playerHandPresenter,
            BonusHandPresenter bonusHandPresenter)
        {
            if (playerHandPresenter == null || bonusHandPresenter == null)
            {
                return BoardSurfaceSnapshot.Empty;
            }

            var rankSets = BuildRankSets(playerHandPresenter);
            var bonusSets = BuildBonusSets(bonusHandPresenter);
            return new BoardSurfaceSnapshot(rankSets, bonusSets);
        }

        private static List<BoardRankSetSnapshot> BuildRankSets(PlayerHandPresenter playerHandPresenter)
        {
            var snapshots = new List<BoardRankSetSnapshot>();
            var rankViewModels = playerHandPresenter.StandardCards;
            if (rankViewModels == null)
            {
                return snapshots;
            }

            foreach (var rankViewModel in rankViewModels)
            {
                if (rankViewModel == null)
                {
                    continue;
                }

                var rank = NormalizeRank(rankViewModel.Rank);
                if (string.IsNullOrWhiteSpace(rank))
                {
                    continue;
                }

                var suits = ResolveSuits(rankViewModel);
                if (suits.Count == 0)
                {
                    continue;
                }

                snapshots.Add(new BoardRankSetSnapshot(rank, suits));
            }

            snapshots.Sort((left, right) => CompareRank(left.Rank, right.Rank));
            return snapshots;
        }

        private static List<BoardBonusSetSnapshot> BuildBonusSets(BonusHandPresenter bonusHandPresenter)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var bonusViewModels = bonusHandPresenter.BonusCards;
            if (bonusViewModels != null)
            {
                foreach (var bonusViewModel in bonusViewModels)
                {
                    if (bonusViewModel == null)
                    {
                        continue;
                    }

                    var bonusType = NormalizeBonusType(bonusViewModel.BonusCardType);
                    if (string.IsNullOrWhiteSpace(bonusType))
                    {
                        continue;
                    }

                    counts.TryGetValue(bonusType, out var current);
                    counts[bonusType] = current + 1;
                }
            }

            var snapshots = new List<BoardBonusSetSnapshot>(counts.Count);
            foreach (var pair in counts)
            {
                snapshots.Add(new BoardBonusSetSnapshot(pair.Key, Mathf.Max(0, pair.Value)));
            }

            snapshots.Sort((left, right) =>
                string.Compare(left.BonusType, right.BonusType, StringComparison.OrdinalIgnoreCase));
            return snapshots;
        }

        private static List<string> ResolveSuits(RankStackViewModel rankViewModel)
        {
            var suits = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cards = rankViewModel.Cards?.CurrentValue;
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    if (card == null)
                    {
                        continue;
                    }

                    var suit = NormalizeSuit(card.Suit);
                    if (string.IsNullOrWhiteSpace(suit) || !seen.Add(suit))
                    {
                        continue;
                    }

                    suits.Add(suit);
                }
            }

            suits.Sort(StringComparer.OrdinalIgnoreCase);
            return suits;
        }

        private static int CompareRank(string left, string right)
        {
            var leftOrder = ResolveRankOrder(left);
            var rightOrder = ResolveRankOrder(right);
            if (leftOrder != rightOrder)
            {
                return leftOrder.CompareTo(rightOrder);
            }

            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static int ResolveRankOrder(string rank)
        {
            return rank switch
            {
                "two" => 2,
                "three" => 3,
                "four" => 4,
                "five" => 5,
                "six" => 6,
                "seven" => 7,
                "eight" => 8,
                "nine" => 9,
                "ten" => 10,
                "jack" => 11,
                "queen" => 12,
                "king" => 13,
                "ace" => 14,
                _ => 100
            };
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank)
                ? string.Empty
                : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit)
                ? string.Empty
                : suit.Trim().ToLowerInvariant();
        }

        private static string NormalizeBonusType(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType)
                ? string.Empty
                : bonusType.Trim();
        }
    }
}
