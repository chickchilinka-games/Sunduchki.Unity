using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View
{
    public partial class PlayerHandHolder
    {
        private void RemoveMissingBonusViews(IReadOnlyCollection<BonusCardViewModel> active)
        {
            var toRemove = new List<BonusCardViewModel>();
            foreach (var entry in _bonusViews.ToArray())
            {
                if (!active.Contains(entry.Key))
                {
                    toRemove.Add(entry.Key);
                }
            }

            foreach (var viewModel in toRemove)
            {
                if (_bonusViews.TryGetValue(viewModel, out var view))
                {
                    _bonusViews.Remove(viewModel);
                    SafeDespawnBonusView(view);
                }
            }
        }

        private IReadOnlyList<RankStackViewModel> BuildStandardLayoutOrder(
            IReadOnlyCollection<RankStackViewModel> activeStandard,
            IReadOnlyList<RankStackViewModel> presenterStandard)
        {
            var ordered = new List<RankStackViewModel>();

            foreach (var entry in _standardViews.ToArray())
            {
                var viewModel = entry.Key;
                var view = entry.Value;
                if (viewModel == null || view == null)
                {
                    continue;
                }

                if (activeStandard.Contains(viewModel) || _pendingStandardRemoval.Contains(viewModel))
                {
                    ordered.Add(viewModel);
                }
            }

            foreach (var viewModel in presenterStandard)
            {
                if (viewModel == null || ordered.Contains(viewModel))
                {
                    continue;
                }

                ordered.Add(viewModel);
            }

            return ordered;
        }

        private void ResetRows()
        {
            _layoutController?.ResetRows();
        }

        private RectTransform GetRowTransform(int slotIndex)
        {
            return _layoutController != null
                ? _layoutController.GetRowTransform(slotIndex)
                : _rowsRoot;
        }

        private RankStackView CreateStandardView(Transform parent, RankStackViewModel viewModel)
        {
            if (_container == null || _standardViewPrefab == null || viewModel == null)
            {
                Debug.LogWarning("[PlayerHand] Failed to create standard stack view: missing DI container or prefab.");
                return null;
            }

            var view = _container.InstantiatePrefabForComponent<RankStackView>(_standardViewPrefab, parent);
            if (view == null)
            {
                return null;
            }

            view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);
            view.ConfigureRuntime(_cardTransferAnimator, ResolveDeckOrigin, ResolveOpponentHandAnchor);
            view.RemovalReady += OnStandardViewRemovalReady;
            view.Initialize(viewModel).Forget();
            return view;
        }

        private void ReleaseAllViews()
        {
            foreach (var view in _standardViews.Values)
            {
                SafeDespawnStandardView(view, string.Empty);
            }

            _standardViews.Clear();
            _pendingStandardRemoval.Clear();
            _pendingStandardRemovalSince.Clear();
            _pendingBonusReceives.Clear();
            _pendingTransferOuts.Clear();

            foreach (var view in _bonusViews.Values)
            {
                SafeDespawnBonusView(view);
            }

            _bonusViews.Clear();
        }

        private void SafeDespawnStandardView(RankStackView view, string rankKey)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                view.RemovalReady -= OnStandardViewRemovalReady;
                Destroy(view.gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed to despawn rank '{rankKey}': {ex.Message}");
            }
        }

        private void SafeDespawnBonusView(BonusCardView view)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                _bonusPool.Despawn(view);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed to despawn bonus view: {ex.Message}");
            }
        }

        private bool TryGetViewModelByView(RankStackView view, out RankStackViewModel viewModel)
        {
            viewModel = null;
            if (view == null)
            {
                return false;
            }

            foreach (var entry in _standardViews)
            {
                if (!ReferenceEquals(entry.Value, view))
                {
                    continue;
                }

                viewModel = entry.Key;
                return true;
            }

            return false;
        }

        private bool TryGetStandardViewByRank(string rank, out RankStackView view, out RankStackViewModel viewModel)
        {
            view = null;
            viewModel = null;
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return false;
            }

            foreach (var entry in _standardViews)
            {
                var candidate = entry.Key;
                var candidateView = entry.Value;
                if (candidate == null || candidateView == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                view = candidateView;
                viewModel = candidate;
                return true;
            }

            return false;
        }

        private bool IsViewModelActive(RankStackViewModel viewModel)
        {
            if (viewModel == null)
            {
                return false;
            }

            foreach (var current in _presenter.StandardCards)
            {
                if (ReferenceEquals(current, viewModel))
                {
                    return true;
                }
            }

            return false;
        }

        private void ForceRebuildLayout()
        {
            _layoutController?.ForceRebuild();
        }
    }
}
