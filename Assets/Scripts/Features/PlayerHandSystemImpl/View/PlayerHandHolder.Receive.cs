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
        private void OnCardTransferred(CardRequestEvent evt)
        {
            EnqueueUiMutation(() => HandleCardTransferred(evt));
        }

        private void HandleCardTransferred(CardRequestEvent evt)
        {
            if (string.IsNullOrWhiteSpace(_localPlayerId))
            {
                _localPlayerId = _presenter.LocalPlayerId;
            }

            var isFromLocal = string.Equals(evt.AskerId, _localPlayerId, StringComparison.OrdinalIgnoreCase);
            if (!isFromLocal)
            {
                return;
            }

            var rank = NormalizeRank(evt.Rank);
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            if (!TryGetStandardViewByRank(rank, out var view, out var viewModel) || view == null)
            {
                RequestLayoutRefresh();
                return;
            }

            var suits = ResolveTransferredSuits(evt, rank);
            var transferCount = evt.Cards != null && evt.Cards.Count > 0
                ? evt.Cards.Count
                : (evt.Count > 0 ? evt.Count : suits.Count);
            if (transferCount <= 0)
            {
                return;
            }

            var actionId = BuildTransferActionId(evt, rank, transferCount);
            if (!TryDispatchTransferOut(rank, suits, transferCount, actionId))
            {
                _pendingTransferOuts.Enqueue(new PendingTransferOut(
                    rank,
                    suits.ToArray(),
                    transferCount,
                    actionId,
                    Time.unscaledTime));
            }
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

                if (suits.Count == 0 && !completedSetForRank)
                {
                    continue;
                }

                if (!TryGetStandardViewModel(group.Key, out var viewModel))
                {
                    RequestLayoutRefresh();
                    continue;
                }

                viewModel.EnqueueReceive(evt.Source, suits, evt.EventSeq, completedSetForRank);
                if (completedSetForRank)
                {
                    completedRankNotified = true;
                }
            }

            if (evt.CompletedSet &&
                !completedRankNotified &&
                !string.IsNullOrWhiteSpace(completedRank) &&
                TryGetStandardViewModel(completedRank, out var completedViewModel) &&
                completedViewModel != null)
            {
                completedViewModel.EnqueueReceive(
                    evt.Source,
                    Array.Empty<string>(),
                    evt.EventSeq,
                    completedSet: true);
            }
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
            if (string.IsNullOrWhiteSpace(bonusType))
            {
                return;
            }

            _pendingBonusReceives.Enqueue(new PendingBonusReceive(
                NormalizeBonus(bonusType),
                source,
                Time.unscaledTime));
        }

        private void FlushPendingBonusReceives()
        {
            if (_pendingBonusReceives.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var pendingCount = _pendingBonusReceives.Count;
            for (var i = 0; i < pendingCount; i++)
            {
                var pending = _pendingBonusReceives.Dequeue();
                if (now - pending.CreatedAt > PendingBonusReceiveMaxAgeSeconds)
                {
                    continue;
                }

                var origin = ResolveReceiveOrigin(pending.Source);
                var card = new BonusCardData(pending.BonusType);
                if (origin == null || !TryAnimateBonusReceive(card, origin))
                {
                    _pendingBonusReceives.Enqueue(pending);
                }
            }
        }

        private bool TryAnimateBonusReceive(BonusCardData card, RectTransform origin)
        {
            var typeKey = NormalizeBonus(card.BonusType);
            foreach (var viewModel in _bonusPresenter.BonusCards.ToList())
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

            foreach (var candidate in _presenter.StandardCards.ToList())
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

        private static IReadOnlyList<string> ResolveTransferredSuits(CardRequestEvent evt, string rank)
        {
            var suits = new List<string>();
            if (evt.Cards == null || evt.Cards.Count == 0)
            {
                return suits;
            }

            for (var i = 0; i < evt.Cards.Count; i++)
            {
                var card = evt.Cards[i];
                if (!string.Equals(NormalizeRank(card.Rank), rank, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suit = NormalizeSuit(card.Suit);
                if (string.IsNullOrWhiteSpace(suit))
                {
                    continue;
                }

                suits.Add(suit);
            }

            return suits;
        }

        private static string BuildTransferActionId(CardRequestEvent evt, string rank, int count)
        {
            return $"{evt.Timestamp.Ticks}:{evt.AskerId}:{evt.TargetId}:{rank}:{count}";
        }

        private void FlushPendingTransferOuts()
        {
            if (_pendingTransferOuts.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var pendingCount = _pendingTransferOuts.Count;
            for (var i = 0; i < pendingCount; i++)
            {
                var pending = _pendingTransferOuts.Dequeue();
                if (now - pending.CreatedAt > PendingTransferOutMaxAgeSeconds)
                {
                    continue;
                }

                if (!TryDispatchTransferOut(pending.Rank, pending.Suits, pending.Count, pending.ActionId))
                {
                    _pendingTransferOuts.Enqueue(pending);
                }
            }
        }

        private bool TryDispatchTransferOut(string rank, IReadOnlyList<string> suits, int transferCount, string actionId)
        {
            var dispatched = false;
            if (TryGetStandardViewModel(rank, out var viewModel) && viewModel != null)
            {
                viewModel.EnqueueTransferOut(suits, transferCount, actionId);
                dispatched = true;
            }

            if (TryGetStandardViewByRank(rank, out var view, out _) && view != null)
            {
                view.EnqueueTransferOutCommand(suits, transferCount, actionId);
                dispatched = true;
            }

            return dispatched;
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
