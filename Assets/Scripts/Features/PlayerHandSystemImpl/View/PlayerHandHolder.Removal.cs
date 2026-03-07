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
        private void BeginOutgoingStandardRemovals(IReadOnlyCollection<RankStackViewModel> active)
        {
            var toRemove = new List<(RankStackViewModel ViewModel, RankStackView View)>();
            foreach (var entry in _standardViews)
            {
                var viewModel = entry.Key;
                var view = entry.Value;
                if (viewModel == null || view == null)
                {
                    toRemove.Add((viewModel, view));
                    continue;
                }

                if (active.Contains(viewModel))
                {
                    continue;
                }

                toRemove.Add((viewModel, view));
            }

            foreach (var removal in toRemove)
            {
                BeginStandardRemoval(removal.ViewModel, removal.View);
            }
        }

        private void BeginStandardRemoval(RankStackViewModel viewModel, RankStackView view)
        {
            if (viewModel == null || view == null)
            {
                FinalizeStandardRemoval(viewModel, view);
                return;
            }

            if (_removingStandard.Contains(viewModel))
            {
                return;
            }

            var rankKey = NormalizeRank(viewModel.Rank);
            if (IsTransferOutInProgress(rankKey))
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: false,
                        waitForTransferOut: true).Forget();
                }

                return;
            }

            if (ShouldWaitTransferOutStart(rankKey))
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    _animationGate?.MarkRemovalStartGracePending(rankKey);
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: true,
                        waitForSetCompletion: false,
                        waitForTransferOut: false).Forget();
                }

                return;
            }

            if (IsPendingSetCompletion(rankKey) && !view.HasCompletedSetAnimation)
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: true,
                        waitForTransferOut: false).Forget();
                }

                return;
            }

            if (view.HasActiveAnimations)
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: false,
                        waitForTransferOut: false).Forget();
                }

                return;
            }

            if (view.HasCompletedSetAnimation)
            {
                FinalizeStandardRemoval(viewModel, view);
                return;
            }

            if (!_removingStandard.Add(viewModel))
            {
                return;
            }

            _animationGate?.MarkRemoving(rankKey);

            if (IsTransferredRank(rankKey))
            {
                view.MarkTransferredOut();
            }

            AnimateAndDespawnStandard(viewModel, view).Forget();
        }

        private async UniTaskVoid WaitAndRetryRemovalAsync(
            RankStackViewModel viewModel,
            RankStackView view,
            bool waitForRemovalStartGrace,
            bool waitForSetCompletion,
            bool waitForTransferOut)
        {
            try
            {
                if (view != null)
                {
                    if (waitForRemovalStartGrace)
                    {
                        await UniTask.WaitUntil(() =>
                            !ShouldWaitTransferOutStart(viewModel?.Rank));
                    }

                    if (waitForTransferOut)
                    {
                        await UniTask.WaitUntil(() => !IsTransferOutInProgress(viewModel?.Rank));
                    }

                    if (waitForSetCompletion)
                    {
                        await UniTask.WaitUntil(() =>
                            view == null ||
                            view.HasCompletedSetAnimation ||
                            !IsPendingSetCompletion(viewModel?.Rank));
                    }

                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                    if (view.HasActiveAnimations)
                    {
                        await view.WaitForIdleAnimationsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var rank = NormalizeRank(viewModel?.Rank);
                Debug.LogWarning($"[PlayerHand] Wait for idle animations failed for rank '{rank}': {ex.Message}");
            }
            finally
            {
                _animationGate?.ClearRemovalStartGracePending(viewModel?.Rank);
                _waitingStandardAnimation.Remove(viewModel);
                RequestLayoutRefresh();
            }
        }

        private async UniTaskVoid AnimateAndDespawnStandard(RankStackViewModel viewModel, RankStackView view)
        {
            var rankKey = NormalizeRank(viewModel?.Rank);
            try
            {
                if (view != null)
                {
                    await view.WaitForExternalAnimationsAsync();
                    if (!view.SkipRemovalAnimation)
                    {
                        await view.PlayTransferRemovalAnimationAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed outgoing animation for rank '{rankKey}': {ex.Message}");
            }
            finally
            {
                FinalizeStandardRemoval(viewModel, view);
                RequestLayoutRefresh();
            }
        }

        private void FinalizeStandardRemoval(RankStackViewModel viewModel, RankStackView view)
        {
            if (viewModel != null)
            {
                _standardViews.Remove(viewModel);
                _removingStandard.Remove(viewModel);
                _waitingStandardAnimation.Remove(viewModel);
                _animationGate?.FinalizeRemoval(viewModel.Rank);
                ClearTransferredRank(viewModel.Rank);
                ClearTransferOutInProgress(viewModel.Rank);
                ClearRemovalStartGrace(viewModel.Rank);
                ClearPendingSetCompletion(viewModel.Rank);
            }

            SafeDespawnStandardView(view, NormalizeRank(viewModel?.Rank));
        }

        private void OnSetCompletionAnimationFinished(RankStackView view)
        {
            if (view == null)
            {
                return;
            }

            if (!TryGetViewModelByView(view, out var viewModel))
            {
                return;
            }

            if (IsViewModelActive(viewModel))
            {
                return;
            }

            if (_removingStandard.Contains(viewModel))
            {
                return;
            }

            if (IsTransferOutInProgress(viewModel.Rank) || view.HasActiveAnimations)
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: false,
                        waitForTransferOut: true).Forget();
                }

                return;
            }

            ClearPendingSetCompletion(viewModel.Rank);
            FinalizeStandardRemoval(viewModel, view);
            RequestLayoutRefresh();
        }

        private void OnStandardViewAnimationsBecameIdle(RankStackView view)
        {
            if (view == null || !_presenter.IsActive.CurrentValue)
            {
                return;
            }

            if (TryGetViewModelByView(view, out var viewModel))
            {
                _animationGate?.CompleteReceiving(viewModel.Rank);
            }

            RequestLayoutRefresh();
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
    }
}
