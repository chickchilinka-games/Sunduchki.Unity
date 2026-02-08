using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.PlayerHandSystemImpl.View;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using Modules.CardRequestSystem.Data;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Services;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.CardRequestSystemImpl.View
{
    public class CardTransferAnimator : MonoBehaviour
    {
        private const string SpriteIdFormat = "Art/Cards/Standard/card_{0}_{1}";
        private static readonly string[] SuitFallbacks = { "clubs", "diamonds", "hearts", "spades" };

        [SerializeField] private RectTransform _animationRoot;
        [SerializeField] private Image _cardPrefab;
        [SerializeField] private RectTransform _opponentHandAnchor;
        [SerializeField] private PlayerHandHolder _localHandHolder;
        [SerializeField] private float _toTargetDuration = 0.45f;
        [SerializeField] private float _fadeOutDuration = 0.2f;
        [SerializeField] private float _staggerDelay = 0.08f;

        private PlayerHandService _handService;
        private AssetService _assetService;

        public RectTransform OpponentHandAnchor => _opponentHandAnchor;
        [Inject]
        public void Construct(
            PlayerHandService handService,
            AssetService assetService)
        {
            _handService = handService;
            _assetService = assetService;
        }

        public void AnimateTransfer(CardRequestEvent evt, string localPlayerId)
        {
            AnimateTransferAsync(evt, localPlayerId).Forget();
        }

        private async UniTaskVoid AnimateTransferAsync(CardRequestEvent evt, string localPlayerId)
        {
            if (string.IsNullOrWhiteSpace(localPlayerId))
            {
                return;
            }

            var isFromLocal = string.Equals(evt.AskerId, localPlayerId, StringComparison.Ordinal);
            var isToLocal = string.Equals(evt.TargetId, localPlayerId, StringComparison.Ordinal);
            if (!isFromLocal && !isToLocal)
            {
                return;
            }

            if (_animationRoot == null || _cardPrefab == null)
            {
                Debug.LogWarning("[CardTransfer] Animation references are not configured.");
                return;
            }

            var rank = NormalizeRank(evt.Rank);
            var suits = isFromLocal ? ResolveTransferSuits(evt, rank, localPlayerId, allowFallback: false) : new List<string>();

            var count = evt.Count > 0 ? evt.Count : 1;
            if (isFromLocal && count <= 0)
            {
                count = evt.Cards != null && evt.Cards.Count > 0 ? evt.Cards.Count : 1;
            }
            var transferScope = new TransferScope();
            try
            {
                if (isFromLocal)
                {
                    _localHandHolder?.MarkRankTransferredOut(rank);
                }

                if (isFromLocal && suits.Count > 0)
                {
                    if (count > suits.Count)
                    {
                        count = suits.Count;
                    }
                    else if (count < suits.Count)
                    {
                        suits = suits.Take(count).ToList();
                    }
                }

                for (var i = 0; i < count; i++)
                {
                    if (isFromLocal)
                    {
                        var hasSuit = suits.Count > 0;
                        var suit = hasSuit ? PickSuit(suits) : PickSuitFallback();
                        await AnimateFromLocalAsync(rank, suit, hasSuit, i, transferScope);
                    }
                    else
                    {
                        await AnimateToLocalAsync(rank, i, transferScope);
                    }
                }
            }
            finally
            {
                if (isFromLocal)
                {
                    _localHandHolder?.CompleteRankTransfer(rank);
                }

                transferScope.Dispose();
            }
        }

        private async UniTask AnimateFromLocalAsync(string rank, string suit, bool useSourceImage, int index, TransferScope transferScope)
        {
            var toAnchor = _opponentHandAnchor;
            if (toAnchor == null)
            {
                return;
            }

            Image sourceImage = null;
            Action restore = null;
            if (useSourceImage &&
                _localHandHolder != null &&
                _localHandHolder.TryGetStandardCardImage(rank, suit, out sourceImage) &&
                sourceImage != null)
            {
                restore = HideImage(sourceImage);
                var view = sourceImage.GetComponentInParent<RankStackView>();
                view?.MarkTransferredOut();
                transferScope?.Ensure(view);
            }

            var spriteResult = await ResolveSpriteAsync(rank, suit, sourceImage);
            if (spriteResult.Sprite == null)
            {
                spriteResult.Handle?.Dispose();
                return;
            }

            var start = sourceImage != null ? sourceImage.rectTransform.position : _animationRoot.position;
            try
            {
                await PlayFlightAsync(spriteResult.Sprite, spriteResult.Handle, start, toAnchor.position, index, fadeOut: true);
            }
            finally
            {
                // Keep the source hidden; it will be shown/removed by the hand snapshot.
            }
        }

        private async UniTask AnimateToLocalAsync(string rank, int index, TransferScope transferScope)
        {
            var fromAnchor = _opponentHandAnchor;
            if (fromAnchor == null)
            {
                return;
            }

            Image targetImage = await WaitForLocalTargetImage(rank);
            Action restore = null;
            RankStackView targetView = null;
            if (targetImage != null)
            {
                restore = HideImage(targetImage);
                var view = targetImage.GetComponentInParent<RankStackView>();
                transferScope?.Ensure(view);
                targetView = view;
            }
            else if (_localHandHolder != null && _localHandHolder.TryGetStandardCardView(rank, out var view))
            {
                targetView = view;
                transferScope?.Ensure(view);
            }

            var spriteResult = await ResolveSpriteAsync(rank, string.Empty, targetImage);
            if (spriteResult.Sprite == null)
            {
                spriteResult.Handle?.Dispose();
                restore?.Invoke();
                return;
            }

            var start = fromAnchor.position;
            var target = targetImage != null
                ? targetImage.rectTransform.position
                : (targetView != null ? targetView.transform.position :
                    _localHandHolder != null ? _localHandHolder.transform.position : _animationRoot.position);
            try
            {
                await PlayFlightAsync(spriteResult.Sprite, spriteResult.Handle, start, target, index, fadeOut: false);
            }
            finally
            {
                restore?.Invoke();
            }
        }

        private async UniTask<Image> WaitForLocalTargetImage(string rank)
        {
            if (_localHandHolder == null)
            {
                return null;
            }

            for (var i = 0; i < 6; i++)
            {
                if (_localHandHolder.TryGetStandardCardImage(rank, out var image) && image != null)
                {
                    return image;
                }

                await UniTask.Delay(TimeSpan.FromMilliseconds(50));
            }

            return null;
        }

        private async UniTask PlayFlightAsync(
            Sprite sprite,
            ManagedAsset<Sprite> handle,
            Vector3 start,
            Vector3 target,
            int index,
            bool fadeOut)
        {
            var view = Instantiate(_cardPrefab, _animationRoot);
            view.sprite = sprite;
            view.color = new Color(1f, 1f, 1f, 1f);
            var canvas = view.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = view.gameObject.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;
            var rect = view.rectTransform;
            rect.position = start;

            if (_staggerDelay > 0f && index > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_staggerDelay * index));
            }

            var sequence = DOTween.Sequence();
            sequence.Append(rect.DOMove(target, _toTargetDuration).SetEase(Ease.InQuad));
            if (fadeOut)
            {
                sequence.Append(view.DOFade(0f, _fadeOutDuration));
            }
            await sequence.AsyncWaitForCompletion();

            Destroy(view.gameObject);
            handle?.Dispose();
        }

        private async UniTask<SpriteResult> ResolveSpriteAsync(string rank, string suit, Image sourceImage)
        {
            if (sourceImage != null && sourceImage.sprite != null)
            {
                return new SpriteResult(sourceImage.sprite, null);
            }

            var resolvedSuit = string.IsNullOrWhiteSpace(suit) ? PickSuit(new List<string>()) : suit;
            var handle = await LoadSpriteAsync(rank, resolvedSuit);
            return new SpriteResult(handle?.Asset, handle);
        }

        private List<string> ResolveTransferSuits(CardRequestEvent evt, string rank, string localPlayerId, bool allowFallback)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cards = evt.Cards;
            if (cards != null && cards.Count > 0)
            {
                foreach (var card in cards)
                {
                    if (!string.Equals(NormalizeRank(card.Rank), rank, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var suit = NormalizeSuit(card.Suit);
                    if (!string.IsNullOrWhiteSpace(suit) && seen.Add(suit))
                    {
                        result.Add(suit);
                    }
                }
            }

            if (result.Count > 0)
            {
                return result;
            }

            if (!allowFallback || string.IsNullOrWhiteSpace(localPlayerId) || _handService == null)
            {
                return new List<string>();
            }

            var state = _handService.ObserveHand(localPlayerId).CurrentValue;
            var matches = state.StandardCards
                .Where(card => string.Equals(NormalizeRank(card.Rank), rank, StringComparison.OrdinalIgnoreCase))
                .Select(card => NormalizeSuit(card.Suit))
                .Where(suit => !string.IsNullOrWhiteSpace(suit))
                .ToList();

            return matches;
        }

        private static Action HideImage(Image image)
        {
            if (image == null)
            {
                return () => { };
            }

            var color = image.color;
            var originalAlpha = color.a;
            image.color = new Color(color.r, color.g, color.b, 0f);
            return () =>
            {
                if (image == null)
                {
                    return;
                }

                var restoreColor = image.color;
                image.color = new Color(restoreColor.r, restoreColor.g, restoreColor.b, originalAlpha);
            };
        }

        private static string PickSuit(IList<string> suits)
        {
            if (suits != null && suits.Count > 0)
            {
                var suit = suits[0];
                suits.RemoveAt(0);
                return suit;
            }

            return PickSuitFallback();
        }

        private static string PickSuitFallback()
        {
            return SuitFallbacks[UnityEngine.Random.Range(0, SuitFallbacks.Length)];
        }

        private async UniTask<ManagedAsset<Sprite>> LoadSpriteAsync(string rank, string suit)
        {
            if (_assetService == null)
            {
                return null;
            }

            var spriteId = string.Format(SpriteIdFormat, rank, suit);
            try
            {
                return await _assetService.Get<Sprite>(spriteId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CardTransfer] Failed to load sprite '{spriteId}': {ex.Message}");
                return null;
            }
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank) ? string.Empty : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit) ? string.Empty : suit.Trim().ToLowerInvariant();
        }

        private readonly struct SpriteResult
        {
            public Sprite Sprite { get; }
            public ManagedAsset<Sprite> Handle { get; }

            public SpriteResult(Sprite sprite, ManagedAsset<Sprite> handle)
            {
                Sprite = sprite;
                Handle = handle;
            }
        }

        private sealed class TransferScope : IDisposable
        {
            private RankStackView _view;
            private IDisposable _scope;

            public void Ensure(RankStackView view)
            {
                if (_scope != null || view == null)
                {
                    return;
                }

                _view = view;
                _scope = view.BeginExternalAnimation();
            }

            public void Dispose()
            {
                _scope?.Dispose();
                _scope = null;
                _view = null;
            }
        }
    }
}
