using System;
using UnityEngine.UI;

namespace Features.PlayerHandSystemImpl.View
{
    public partial class PlayerHandHolder
    {
        public bool TryGetStandardCardImage(string rank, string suit, out Image image)
        {
            image = null;
            var rankKey = NormalizeRank(rank);
            foreach (var viewModel in _standardViews.Keys)
            {
                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out var view) &&
                    view != null &&
                    view.TryGetCardImage(suit, out image))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetStandardCardImage(string rank, out Image image)
        {
            return TryGetStandardCardImage(rank, string.Empty, out image);
        }

        public bool TryGetStandardCardView(string rank, out RankStackView view)
        {
            view = null;
            var rankKey = NormalizeRank(rank);
            foreach (var viewModel in _standardViews.Keys)
            {
                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out view) && view != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetBonusView(string bonusType, out BonusCardView view)
        {
            view = null;
            var typeKey = NormalizeBonus(bonusType);
            foreach (var viewModel in _bonusViews.Keys)
            {
                if (!string.Equals(viewModel.BonusCardType, typeKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_bonusViews.TryGetValue(viewModel, out view) && view != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
