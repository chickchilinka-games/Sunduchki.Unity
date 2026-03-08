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
            foreach (var entry in _bonusViews)
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

        private IReadOnlyList<RankStackViewModel> BuildStandardLayoutOrder(IReadOnlyCollection<RankStackViewModel> activeStandard)
        {
            var ordered = new List<RankStackViewModel>();

            foreach (var entry in _standardViews)
            {
                var viewModel = entry.Key;
                var view = entry.Value;
                if (viewModel == null || view == null)
                {
                    continue;
                }

                if (activeStandard.Contains(viewModel) ||
                    _removingStandard.Contains(viewModel) ||
                    _waitingStandardAnimation.Contains(viewModel))
                {
                    ordered.Add(viewModel);
                }
            }

            foreach (var viewModel in _presenter.StandardCards)
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
            view.ConfigureReceiveOrigins(ResolveDeckOrigin, ResolveOpponentHandAnchor);
            view.SetCompletionAnimationFinished += OnSetCompletionAnimationFinished;
            view.AnimationsBecameIdle += OnStandardViewAnimationsBecameIdle;
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
            _removingStandard.Clear();
            _waitingStandardAnimation.Clear();
            _animationGate?.Clear();
            _receiveBuffer?.Clear();

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
                view.SetCompletionAnimationFinished -= OnSetCompletionAnimationFinished;
                view.AnimationsBecameIdle -= OnStandardViewAnimationsBecameIdle;
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

        private void ForceRebuildLayout()
        {
            _layoutController?.ForceRebuild();
        }
    }
}

