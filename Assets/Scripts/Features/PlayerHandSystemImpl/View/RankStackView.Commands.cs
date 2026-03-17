using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Presentation.Data;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View
{
    public partial class RankStackView
    {
        private async UniTaskVoid RunCommandLoopIfNeeded()
        {
            if (_isCommandLoopRunning || _viewModel == null)
            {
                return;
            }

            _isCommandLoopRunning = true;
            try
            {
                while (_viewModel != null)
                {
                    if (_queuedCommands.Count == 0)
                    {
                        break;
                    }

                    var token = _bindingsCts?.Token ?? CancellationToken.None;
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        var command = _queuedCommands.Dequeue();
                        await ExecuteCommandAsync(command, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PlayerHand] Rank command failed for '{_viewModel?.Rank}': {ex.Message}");
                    }
                }
            }
            finally
            {
                _isCommandLoopRunning = false;
                NotifyRemovalReadyIfPossible();
            }
        }

        private void EnqueueViewModelCommands()
        {
            if (_viewModel == null)
            {
                return;
            }

            while (_viewModel.TryDequeueCommand(out var command))
            {
                EnqueueCommand(command);
            }
        }

        private void EnqueueCommand(RankStackCommand command)
        {
            _queuedCommands.Enqueue(command);
        }

        private UniTask ExecuteCommandAsync(RankStackCommand command, CancellationToken cancellationToken)
        {
            return command.Type switch
            {
                RankStackCommandType.ApplySnapshot => ExecuteApplySnapshotAsync(command, cancellationToken),
                RankStackCommandType.Receive => ExecuteReceiveAsync(command, cancellationToken),
                RankStackCommandType.TransferOut => ExecuteTransferOutAsync(command, cancellationToken),
                RankStackCommandType.SetComplete => ExecuteSetCompleteAsync(cancellationToken),
                _ => UniTask.CompletedTask
            };
        }

        private async UniTask ExecuteApplySnapshotAsync(RankStackCommand command, CancellationToken cancellationToken)
        {
            var expectedVersion = _bindingVersion;
            if (command.Revision > 0 && command.Revision < _lastAppliedSnapshotRevision)
            {
                return;
            }

            if (command.Revision > 0)
            {
                _lastAppliedSnapshotRevision = command.Revision;
            }

            TrackRemovedSnapshotSuits(command.Suits);
            await UpdateCardsAsync(command.Suits, expectedVersion, cancellationToken);
            _removalNotified = false;
            if (_setCompletePlayed && HasReadyCards())
            {
                _setCompletePlayed = false;
            }
        }

        private async UniTask ExecuteReceiveAsync(RankStackCommand command, CancellationToken cancellationToken)
        {
            var expectedVersion = _bindingVersion;
            if (command.Suits == null || command.Suits.Count == 0)
            {
                return;
            }

            var receiveSuits = NormalizeSuits(command.Suits);
            foreach (var pendingSuit in receiveSuits)
            {
                _activeReceiveSuits.Add(pendingSuit);
            }

            try
            {
                for (var i = 0; i < receiveSuits.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested || expectedVersion != _bindingVersion)
                    {
                        return;
                    }

                    var suit = receiveSuits[i];
                    if (string.IsNullOrWhiteSpace(suit))
                    {
                        continue;
                    }

                    var cardIndex = await ResolveCardIndexAsync(suit, cancellationToken);
                    if (cardIndex < 0)
                    {
                        _activeReceiveSuits.Remove(suit);
                        continue;
                    }

                    await PlayReceiveAnimationAsync(cardIndex, command.Source, suit, cancellationToken);
                }
            }
            finally
            {
                foreach (var suit in receiveSuits)
                {
                    _activeReceiveSuits.Remove(suit);
                }
            }
        }

        private async UniTask ExecuteTransferOutAsync(RankStackCommand command, CancellationToken cancellationToken)
        {
            var expectedVersion = _bindingVersion;
            var pendingSuits = NormalizeSuits(command.Suits);
            var transferCount = command.Count > 0 ? command.Count : pendingSuits.Count;
            if (transferCount <= 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(command.ActionId) &&
                !_processedTransferActionIds.Add(command.ActionId))
            {
                return;
            }

            CleanupRecentlyRemovedSnapshotSuits();
            var consumedIndices = new HashSet<int>();
            for (var i = 0; i < transferCount; i++)
            {
                if (cancellationToken.IsCancellationRequested || expectedVersion != _bindingVersion)
                {
                    return;
                }

                if (TryConsumeRecentlyRemovedSuit(pendingSuits, out var removedBySnapshotSuit))
                {
                    await PlayTransferOutAnimationAsync(-1, removedBySnapshotSuit, i, cancellationToken);
                    continue;
                }

                if (!TryResolveTransferTarget(pendingSuits, consumedIndices, out var cardIndex, out var suit))
                {
                    if (pendingSuits.Count > 0)
                    {
                        suit = pendingSuits[0];
                        pendingSuits.RemoveAt(0);
                    }

                    await PlayTransferOutAnimationAsync(-1, suit, i, cancellationToken);
                    continue;
                }

                await PlayTransferOutAnimationAsync(cardIndex, suit, i, cancellationToken);
                consumedIndices.Add(cardIndex);
                MarkCardTransferred(cardIndex, suit);
            }

            if (!HasReadyCards())
            {
                _removalRequested = true;
                NotifyRemovalReadyIfPossible();
            }
        }

        private async UniTask ExecuteSetCompleteAsync(CancellationToken cancellationToken)
        {
            var expectedVersion = _bindingVersion;
            if (cancellationToken.IsCancellationRequested ||
                expectedVersion != _bindingVersion ||
                _setCompletePlayed)
            {
                return;
            }

            if (_canvasGroup == null)
            {
                MarkSetCompleteAndRequestRemoval();
                return;
            }

            await _tweenAnimator.PlaySetCompleteAsync(
                _canvasGroup,
                _setCompleteFlashDuration,
                _setCompleteFadeDuration,
                _tweenTimeoutBuffer,
                cancellationToken);

            MarkSetCompleteAndRequestRemoval();
        }

        private void MarkSetCompleteAndRequestRemoval()
        {
            _setCompletePlayed = true;
            _removalRequested = true;
            NotifyRemovalReadyIfPossible();
        }
    }
}
