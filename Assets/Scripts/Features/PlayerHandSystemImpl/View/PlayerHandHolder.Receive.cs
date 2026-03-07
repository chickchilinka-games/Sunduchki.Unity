using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.CardRequestSystemImpl.View;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Modules.CardRequestSystem.Data;
using Modules.PlayerHand.Data;
using UnityEngine;

namespace Features.PlayerHandSystemImpl.View
{
    public partial class PlayerHandHolder
    {
        private void OnCardRequested(CardRequestEvent evt)
        {
            // Placeholder for future UI hooks.
        }

        private void OnCardTransferred(CardRequestEvent evt)
        {
            var snapshot = SnapshotCardRequestEvent(evt);
            EnqueueUiMutation(() => HandleCardTransferred(snapshot));
        }

        private void HandleCardTransferred(CardRequestEvent evt)
        {
            if (_cardTransferAnimator == null)
            {
                _cardTransferAnimator = FindObjectOfType<CardTransferAnimator>();
                if (_cardTransferAnimator == null)
                {
                    return;
                }
            }

            if (string.Equals(evt.TargetId, "chest", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_localPlayerId))
            {
                _localPlayerId = _presenter.LocalPlayerId;
            }

            _cardTransferAnimator.AnimateTransfer(evt, _localPlayerId);
        }

        private void OnCardsReceived(CardsReceivedEvent evt)
        {
            var snapshot = SnapshotCardsReceivedEvent(evt);
            EnqueueUiMutation(() => HandleCardsReceived(snapshot));
        }

        private void HandleCardsReceived(CardsReceivedEvent evt)
        {
            DispatchStandardReceives(evt);
            DispatchBonusReceives(evt);
            FlushPendingStandardReceives();
            FlushPendingBonusReceives();
        }

        private void DispatchStandardReceives(CardsReceivedEvent evt)
        {
            if ((evt.StandardCards == null || evt.StandardCards.Count == 0) &&
                (!evt.CompletedSet || string.IsNullOrWhiteSpace(evt.CompletedSetRank)))
            {
                return;
            }

            var completedRank = NormalizeRank(evt.CompletedSetRank);
            var completedRankNotified = false;

            var grouped = (evt.StandardCards ?? Array.Empty<StandardCardData>())
                .Where(card => !string.IsNullOrWhiteSpace(card.Rank))
                .GroupBy(card => NormalizeRank(card.Rank));

            foreach (var group in grouped)
            {
                var suits = group
                    .Select(card => NormalizeSuit(card.Suit))
                    .Where(suit => !string.IsNullOrWhiteSpace(suit))
                    .ToList();

                var completedSetForRank = evt.CompletedSet &&
                                          string.Equals(group.Key, completedRank, StringComparison.OrdinalIgnoreCase);
                if (completedSetForRank &&
                    (IsTransferredRank(group.Key) || IsTransferOutInProgress(group.Key)))
                {
                    completedSetForRank = false;
                }

                if (suits.Count == 0 && !completedSetForRank)
                {
                    continue;
                }

                if (!TryGetStandardViewModel(group.Key, out var viewModel) ||
                    !TryGetStandardCardView(group.Key, out _))
                {
                    EnqueuePendingStandardReceive(
                        group.Key,
                        suits,
                        evt.Source,
                        evt.EventSeq,
                        completedSetForRank);
                    continue;
                }

                if (completedSetForRank)
                {
                    MarkPendingSetCompletion(group.Key);
                    completedRankNotified = true;
                }

                if (suits.Count > 0)
                {
                    _animationGate?.MarkReceiving(group.Key);
                }

                viewModel.NotifyCardsReceived(evt.Source, suits, evt.EventSeq, completedSetForRank);
            }

            if (evt.CompletedSet &&
                !completedRankNotified &&
                !string.IsNullOrWhiteSpace(completedRank))
            {
                var transferOutInProgress = IsTransferredRank(completedRank) || IsTransferOutInProgress(completedRank);
                if (transferOutInProgress)
                {
                    return;
                }

                if (TryGetStandardViewModel(completedRank, out var completedViewModel) &&
                    TryGetStandardCardView(completedRank, out _))
                {
                    MarkPendingSetCompletion(completedRank);
                    completedViewModel.NotifyCardsReceived(
                        evt.Source,
                        Array.Empty<string>(),
                        evt.EventSeq,
                        completedSet: true);
                }
                else
                {
                    EnqueuePendingStandardReceive(
                        completedRank,
                        Array.Empty<string>(),
                        evt.Source,
                        evt.EventSeq,
                        completedSet: true);
                }
            }
        }

        private void EnqueuePendingStandardReceive(
            string rank,
            IReadOnlyList<string> suits,
            string source,
            long eventSeq,
            bool completedSet)
        {
            _receiveBuffer?.EnqueuePendingStandardReceive(rank, suits, source, eventSeq, completedSet);
        }

        private void FlushPendingStandardReceives()
        {
            _receiveBuffer?.FlushPendingStandardReceives(
                CanDispatchStandardReceive,
                DispatchBufferedStandardReceive);
        }

        private IReadOnlyList<string> GetPendingStandardSuitsForRank(string rank)
        {
            return _receiveBuffer != null
                ? _receiveBuffer.GetPendingStandardSuitsForRank(rank)
                : Array.Empty<string>();
        }

        private bool CanDispatchStandardReceive(string rank)
        {
            return TryGetStandardViewModel(rank, out _) && TryGetStandardCardView(rank, out _);
        }

        private bool DispatchBufferedStandardReceive(HandReceiveBuffer.PendingStandardDispatch dispatch)
        {
            if (!TryGetStandardViewModel(dispatch.Rank, out var viewModel))
            {
                return false;
            }

            var completedSetForRank = dispatch.CompletedSet;
            if (completedSetForRank &&
                (IsTransferredRank(dispatch.Rank) || IsTransferOutInProgress(dispatch.Rank)))
            {
                completedSetForRank = false;
            }

            if (completedSetForRank)
            {
                MarkPendingSetCompletion(dispatch.Rank);
            }

            if (dispatch.Suits.Count > 0)
            {
                _animationGate?.MarkReceiving(dispatch.Rank);
            }

            viewModel.NotifyCardsReceived(
                dispatch.Source,
                dispatch.Suits,
                dispatch.EventSeq,
                completedSetForRank);

            return true;
        }

        private void DispatchBonusReceives(CardsReceivedEvent evt)
        {
            if (evt.BonusCards == null || evt.BonusCards.Count == 0)
            {
                return;
            }

            foreach (var card in evt.BonusCards)
            {
                var origin = ResolveReceiveOrigin(evt.Source);
                if (origin == null || !TryAnimateBonusReceive(card, origin))
                {
                    EnqueuePendingBonusReceive(card.BonusType, evt.Source);
                }
            }
        }

        private void EnqueuePendingBonusReceive(string bonusType, string source)
        {
            _receiveBuffer?.EnqueuePendingBonusReceive(bonusType, source);
        }

        private void FlushPendingBonusReceives()
        {
            _receiveBuffer?.FlushPendingBonusReceives(ResolveReceiveOrigin, TryAnimateBonusReceive);
        }

        private bool TryAnimateBonusReceive(BonusCardData card, RectTransform origin)
        {
            var typeKey = NormalizeBonus(card.BonusType);
            foreach (var viewModel in _bonusPresenter.BonusCards)
            {
                if (!string.Equals(viewModel.BonusCardType, typeKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_bonusViews.TryGetValue(viewModel, out var view) && view != null)
                {
                    view.PlayReceiveFromAsync(origin).Forget();
                    return true;
                }
            }

            return false;
        }

        private bool TryGetStandardViewModel(string rank, out RankStackViewModel viewModel)
        {
            viewModel = null;
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return false;
            }

            foreach (var candidate in _presenter.StandardCards)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                viewModel = candidate;
                return true;
            }

            return false;
        }

        private RectTransform ResolveReceiveOrigin(string source)
        {
            if (string.Equals(source, "deck", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveDeckOrigin();
            }

            return ResolveOpponentHandAnchor();
        }

        private RectTransform ResolveDeckOrigin()
        {
            if (_deckOrigin != null)
            {
                return _deckOrigin;
            }

            var fallback = _rowsRoot != null ? _rowsRoot : transform as RectTransform;
            if (fallback == null)
            {
                return null;
            }

            if (!_warnedMissingDeckOrigin)
            {
                _warnedMissingDeckOrigin = true;
                Debug.LogWarning("[PlayerHand] Deck origin is not assigned, using hand root as fallback.");
            }

            return fallback;
        }

        private RectTransform ResolveOpponentHandAnchor()
        {
            if (_opponentHandAnchor != null)
            {
                return _opponentHandAnchor;
            }

            if (_cachedOpponentAnchor != null)
            {
                return _cachedOpponentAnchor;
            }

            var animator = _cardTransferAnimator ?? FindObjectOfType<CardTransferAnimator>();
            _cachedOpponentAnchor = animator != null ? animator.OpponentHandAnchor : null;
            return _cachedOpponentAnchor;
        }
    }
}
