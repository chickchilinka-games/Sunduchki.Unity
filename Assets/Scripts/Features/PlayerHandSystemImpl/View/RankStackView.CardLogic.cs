using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Modules.AssetSystem.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Features.PlayerHandSystemImpl.View
{
    public partial class RankStackView
    {
        private async UniTask UpdateCardsAsync(
            IReadOnlyList<string> suits,
            int expectedVersion,
            CancellationToken token)
        {
            if (expectedVersion != _bindingVersion || token.IsCancellationRequested)
            {
                return;
            }

            if (_cards == null || _cards.Length == 0 || _cardSpriteResolver == null || _viewModel == null)
            {
                return;
            }

            var rank = NormalizeRank(_viewModel.Rank);
            if (string.IsNullOrWhiteSpace(rank))
            {
                _resolvedSuits.Clear();
                _hiddenReceiveSuits.Clear();
                _pendingAutoRevealSuits.Clear();
                ReleaseCardSprites();
                DisableAllCards();
                return;
            }

            var normalizedSuits = NormalizeSuits(suits);
            var previousSuits = _resolvedSuits.ToList();
            var previousSuitSet = new HashSet<string>(previousSuits, StringComparer.OrdinalIgnoreCase);

            _resolvedSuits.Clear();
            _resolvedSuits.AddRange(normalizedSuits);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            ReleaseCardSprites();
            DisableAllCards();
            _hiddenReceiveSuits.Clear();

            var max = Mathf.Min(normalizedSuits.Count, _cards.Length);
            for (var index = 0; index < max; index++)
            {
                var card = _cards[index];
                if (card == null)
                {
                    continue;
                }

                var suit = normalizedSuits[index];
                var handle = await LoadSpriteAsync(rank, suit, expectedVersion, token);
                if (expectedVersion != _bindingVersion || token.IsCancellationRequested)
                {
                    handle?.Dispose();
                    return;
                }

                if (handle?.Asset == null)
                {
                    continue;
                }

                _cardSprites.Add(handle);
                card.sprite = handle.Asset;
                card.enabled = true;
                card.gameObject.SetActive(true);

                var isNewSuit = previousSuitSet.Count > 0 && !previousSuitSet.Contains(suit);
                var hiddenForReceive =
                    _activeReceiveSuits.Contains(suit) ||
                    _viewModel.IsSuitPendingReceive(suit) ||
                    _pendingAutoRevealSuits.ContainsKey(suit);

                if (!hiddenForReceive && isNewSuit)
                {
                    hiddenForReceive = true;
                    _pendingAutoRevealSuits[suit] = Time.unscaledTime;
                    ScheduleAutoRevealSuitAsync(suit, expectedVersion).Forget();
                }
                else if (!hiddenForReceive)
                {
                    _pendingAutoRevealSuits.Remove(suit);
                }

                if (hiddenForReceive)
                {
                    _hiddenReceiveSuits.Add(suit);
                }

                SetImageAlpha(card, hiddenForReceive ? 0f : 1f);
            }

            var activeSuits = new HashSet<string>(normalizedSuits, StringComparer.OrdinalIgnoreCase);
            var stalePendingReveal = _pendingAutoRevealSuits.Keys.Where(suit => !activeSuits.Contains(suit)).ToList();
            foreach (var staleSuit in stalePendingReveal)
            {
                _pendingAutoRevealSuits.Remove(staleSuit);
                _hiddenReceiveSuits.Remove(staleSuit);
            }

            CacheCardPositions();
            SetTint(_button != null && _button.interactable ? Color.white : DisabledTint);
        }

        private async UniTaskVoid ScheduleAutoRevealSuitAsync(string suit, int expectedVersion)
        {
            if (string.IsNullOrWhiteSpace(suit))
            {
                return;
            }

            try
            {
                var token = _bindingsCts?.Token ?? CancellationToken.None;
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_newCardAutoRevealDelay),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (expectedVersion != _bindingVersion || _viewModel == null)
            {
                return;
            }

            if (!_pendingAutoRevealSuits.ContainsKey(suit))
            {
                return;
            }

            if (_activeReceiveSuits.Contains(suit) || _viewModel.IsSuitPendingReceive(suit))
            {
                return;
            }

            if (!TryResolveCardIndexBySuit(suit, out var index) || !IsCardReady(index))
            {
                return;
            }

            var card = _cards[index];
            if (card != null)
            {
                SetImageAlpha(card, 1f);
            }

            _pendingAutoRevealSuits.Remove(suit);
            _hiddenReceiveSuits.Remove(suit);
        }

        private async UniTask<ManagedAsset<Sprite>> LoadSpriteAsync(
            string rank,
            string suit,
            int expectedVersion,
            CancellationToken token)
        {
            if (_cardSpriteResolver == null || expectedVersion != _bindingVersion || token.IsCancellationRequested)
            {
                return null;
            }

            var handle = await _cardSpriteResolver.ResolveStandardCardAsync(rank, suit);
            if (token.IsCancellationRequested)
            {
                handle?.Dispose();
                return null;
            }

            return handle;
        }

        private async UniTask<int> ResolveCardIndexAsync(string normalizedSuit, CancellationToken token)
        {
            var startedAt = Time.unscaledTime;
            while (!token.IsCancellationRequested)
            {
                if (TryGetReadyCardIndex(normalizedSuit, out var index))
                {
                    return index;
                }

                if (Time.unscaledTime - startedAt >= _resolveCardTimeout)
                {
                    Debug.LogWarning($"[PlayerHand] Receive resolve timeout for rank '{_viewModel?.Rank}', suit '{normalizedSuit}'.");
                    return -1;
                }

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_resolveCardRetryDelay),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    token);
            }

            return -1;
        }

        private bool TryResolveTransferTarget(
            List<string> pendingSuits,
            HashSet<int> consumedIndices,
            out int cardIndex,
            out string suit)
        {
            cardIndex = -1;
            suit = string.Empty;

            if (pendingSuits.Count > 0)
            {
                var candidateSuit = NormalizeSuit(pendingSuits[0]);
                pendingSuits.RemoveAt(0);
                if (TryResolveCardIndexBySuit(candidateSuit, out var resolvedBySuit) &&
                    IsCardReady(resolvedBySuit) &&
                    !consumedIndices.Contains(resolvedBySuit))
                {
                    cardIndex = resolvedBySuit;
                    suit = candidateSuit;
                    return true;
                }
            }

            for (var i = _cards.Length - 1; i >= 0; i--)
            {
                if (consumedIndices.Contains(i))
                {
                    continue;
                }

                if (!IsCardReady(i))
                {
                    continue;
                }

                cardIndex = i;
                if (i >= 0 && i < _resolvedSuits.Count)
                {
                    suit = _resolvedSuits[i];
                }

                return true;
            }

            return false;
        }

        private async UniTask PlayReceiveAnimationAsync(int cardIndex, string source, string suit, CancellationToken token)
        {
            if (token.IsCancellationRequested || !IsCardReady(cardIndex))
            {
                return;
            }

            var card = _cards[cardIndex];
            var rect = card.rectTransform;
            CacheCardPositions();

            var basePos = _cardBasePositions.TryGetValue(card, out var cached) ? cached : rect.anchoredPosition;
            var targetWorld = rect.position;
            var receiveOrigin = ResolveReceiveOrigin(source);

            await _tweenAnimator.PlayReceiveAsync(
                new RankStackReceiveTweenRequest(
                    card,
                    receiveOrigin,
                    basePos,
                    ResolveReceiveOffset(source),
                    targetWorld,
                    _receiveDuration,
                    _tweenTimeoutBuffer),
                token);

            if (!string.IsNullOrWhiteSpace(suit))
            {
                _hiddenReceiveSuits.Remove(suit);
                _pendingAutoRevealSuits.Remove(suit);
            }
        }

        private async UniTask PlayTransferOutAnimationAsync(int cardIndex, string suit, int sequenceIndex, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var card = cardIndex >= 0 && IsCardReady(cardIndex)
                ? _cards[cardIndex]
                : null;
            if (_cardTransferAnimator != null)
            {
                await _cardTransferAnimator.AnimateToOpponentAsync(
                    card,
                    NormalizeRank(_viewModel?.Rank),
                    NormalizeSuit(suit),
                    sequenceIndex,
                    token,
                    _rectTransform != null ? (Vector3?)_rectTransform.position : null);
                return;
            }

            var target = _opponentReceiveOriginProvider?.Invoke();
            if (target == null)
            {
                return;
            }

            if (card == null)
            {
                return;
            }

            await _tweenAnimator.PlayTransferFallbackAsync(
                card,
                target.position,
                _receiveDuration,
                _tweenTimeoutBuffer,
                token);
        }

        private void MarkCardTransferred(int cardIndex, string suit)
        {
            if (cardIndex < 0 || _cards == null || cardIndex >= _cards.Length)
            {
                return;
            }

            var card = _cards[cardIndex];
            if (card != null)
            {
                DOTween.Kill(card);
                DOTween.Kill(card.rectTransform);
                card.gameObject.SetActive(false);
                card.enabled = false;
                SetImageAlpha(card, 1f);
            }

            var normalizedSuit = NormalizeSuit(suit);
            if (!string.IsNullOrWhiteSpace(normalizedSuit))
            {
                _hiddenReceiveSuits.Remove(normalizedSuit);
                _pendingAutoRevealSuits.Remove(normalizedSuit);
                for (var i = 0; i < _resolvedSuits.Count; i++)
                {
                    if (!string.Equals(_resolvedSuits[i], normalizedSuit, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    _resolvedSuits.RemoveAt(i);
                    break;
                }
            }
        }

        private RectTransform ResolveReceiveOrigin(string source)
        {
            if (string.Equals(source, "deck", StringComparison.OrdinalIgnoreCase))
            {
                return _deckReceiveOriginProvider?.Invoke();
            }

            return _opponentReceiveOriginProvider?.Invoke();
        }

        private Vector2 ResolveReceiveOffset(string source)
        {
            if (string.Equals(source, "deck", StringComparison.OrdinalIgnoreCase))
            {
                return new Vector2(0f, _receiveOffsetYFromDeck);
            }

            return new Vector2(_receiveOffsetXFromOpponent, 0f);
        }

        private bool TryGetReadyCardIndex(string normalizedSuit, out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(normalizedSuit))
            {
                return false;
            }

            if (!TryResolveCardIndexBySuit(normalizedSuit, out var suitIndex))
            {
                return false;
            }

            if (!IsCardReady(suitIndex))
            {
                return false;
            }

            index = suitIndex;
            return true;
        }

        private bool TryResolveCardIndexBySuit(string normalizedSuit, out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(normalizedSuit) || _resolvedSuits.Count == 0 || _cards == null)
            {
                return false;
            }

            var max = Mathf.Min(_resolvedSuits.Count, _cards.Length);
            for (var i = 0; i < max; i++)
            {
                if (!string.Equals(_resolvedSuits[i], normalizedSuit, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                index = i;
                return true;
            }

            return false;
        }

        private bool HasReadyCards()
        {
            if (_cards == null)
            {
                return false;
            }

            for (var i = 0; i < _cards.Length; i++)
            {
                if (IsCardReady(i))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCardReady(int cardIndex)
        {
            if (_cards == null || cardIndex < 0 || cardIndex >= _cards.Length)
            {
                return false;
            }

            var card = _cards[cardIndex];
            return card != null && card.gameObject.activeSelf && card.enabled && card.sprite != null;
        }
    }
}
