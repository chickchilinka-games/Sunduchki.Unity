using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.PlayerHandSystemImpl.Presentation.Data;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Features.PlayerHandSystemImpl.Utils;
using Modules.AssetSystem.Models;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.PlayerHandSystemImpl.View
{
    public class RankStackView : MonoBehaviour
    {
        private static readonly Color DisabledTint = new(0.65f, 0.65f, 0.65f, 1f);

        [SerializeField] private Image[] _cards;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;
        [Header("Animation")]
        [SerializeField] private float _receiveOffsetY = 24f;
        [SerializeField] private float _receiveOffsetXFromOpponent = 36f;
        [SerializeField] private float _receiveOffsetYFromDeck = -32f;
        [SerializeField, Min(0.01f)] private float _receiveDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float _transferShakeDuration = 0.12f;
        [SerializeField] private float _transferShakeStrength = 6f;
        [SerializeField, Min(0.01f)] private float _transferFadeDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float _setCompleteFlashDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float _setCompleteFadeDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float _tweenTimeoutBuffer = 0.35f;

        private readonly List<ManagedAsset<Sprite>> _cardSprites = new();
        private readonly Dictionary<Image, Vector2> _cardBasePositions = new();
        private readonly List<string> _resolvedSuits = new();
        private readonly HashSet<string> _pendingReceiveSuits = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<RankCardsReceivedEvent> _deferredReceiveBatches = new();
        private readonly SemaphoreSlim _animationGate = new(1, 1);
        private Func<RectTransform> _deckReceiveOriginProvider;
        private Func<RectTransform> _opponentReceiveOriginProvider;

        private CardSpriteResolver _cardSpriteResolver;
        private RankStackViewModel _viewModel;
        private CompositeDisposable _bindings;
        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private bool _hasCompletedSetAnimation;
        private bool _suppressRemovalAnimation;
        private bool _isAnimationRunning;
        private bool _isDrainingReceiveQueue;
        private int _externalAnimationCount;
        private int _bindingVersion;
        private CancellationTokenSource _bindingsCts;

        public bool HasCompletedSetAnimation => _hasCompletedSetAnimation;
        public bool HasActiveAnimations => _isAnimationRunning || _externalAnimationCount > 0;
        public bool SkipRemovalAnimation => _suppressRemovalAnimation;
        public event Action<RankStackView> SetCompletionAnimationFinished;
        public event Action<RankStackView> AnimationsBecameIdle;

        [Inject]
        public void Construct(CardSpriteResolver cardSpriteResolver)
        {
            _cardSpriteResolver = cardSpriteResolver;
        }

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            _rectTransform = transform as RectTransform;
            if (_rectTransform != null)
            {
                _baseAnchoredPosition = _rectTransform.anchoredPosition;
            }
        }

        private void OnDestroy()
        {
            ResetBindings();
            ReleaseCardSprites();
        }

        public async UniTask Initialize(RankStackViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            ResetBindings();
            ResetAnimations();

            _viewModel = viewModel;
            _bindingsCts = new CancellationTokenSource();
            _bindings = new CompositeDisposable();
            var expectedVersion = _bindingVersion;
            var token = _bindingsCts.Token;

            await UpdateCardsAsync(viewModel.Cards.CurrentValue, expectedVersion, token);

            viewModel.Cards
                .Subscribe(cards => UpdateCardsAsync(cards, expectedVersion, token).Forget())
                .AddTo(_bindings);

            viewModel.CanPress
                .Subscribe(SetInteractable)
                .AddTo(_bindings);

            viewModel.ReceiveQueued
                .Subscribe(_ => DrainReceiveQueueAsync().Forget())
                .AddTo(_bindings);
            DrainReceiveQueueAsync().Forget();

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _viewModel?.Press());
            }
        }

        public void ResetView()
        {
            ResetBindings();
            ResetAnimations();
            ReleaseCardSprites();
            DisableAllCards();

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = false;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        public UniTask PlayTransferRemovalAnimationAsync()
        {
            return EnqueueAnimation(PlayTransferRemovalSequenceAsync);
        }

        public void MarkTransferredOut()
        {
            _suppressRemovalAnimation = true;
        }

        public IDisposable BeginExternalAnimation()
        {
            _externalAnimationCount++;
            return new ExternalAnimationScope(EndExternalAnimation);
        }

        public UniTask WaitForExternalAnimationsAsync()
        {
            return _externalAnimationCount == 0
                ? UniTask.CompletedTask
                : UniTask.WaitUntil(() => _externalAnimationCount == 0);
        }

        public UniTask WaitForIdleAnimationsAsync()
        {
            return HasActiveAnimations
                ? UniTask.WaitUntil(() => !HasActiveAnimations)
                : UniTask.CompletedTask;
        }

        public void ConfigureReceiveOrigins(
            Func<RectTransform> deckReceiveOriginProvider,
            Func<RectTransform> opponentReceiveOriginProvider)
        {
            _deckReceiveOriginProvider = deckReceiveOriginProvider;
            _opponentReceiveOriginProvider = opponentReceiveOriginProvider;
        }

        public bool TryGetCardImage(string suit, out Image image)
        {
            image = null;
            if (_cards == null || _cards.Length == 0)
            {
                return false;
            }

            var normalizedSuit = NormalizeSuit(suit);
            int index;
            if (!string.IsNullOrWhiteSpace(normalizedSuit))
            {
                if (!TryResolveCardIndexBySuit(normalizedSuit, out index) || !IsCardReady(index))
                {
                    return false;
                }
            }
            else
            {
                if (!TryGetLastReadyCardIndex(out index))
                {
                    return false;
                }
            }

            image = _cards[index];
            return image != null;
        }

        public void SetTint(Color color)
        {
            ApplyTint(color);
        }

        private async UniTask UpdateCardsAsync(
            IReadOnlyList<StandardCardItemViewModel> cards,
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
                _pendingReceiveSuits.Clear();
                ReleaseCardSprites();
                DisableAllCards();
                return;
            }

            var previousSuits = _resolvedSuits.ToList();
            var previousSuitSet = new HashSet<string>(previousSuits, StringComparer.OrdinalIgnoreCase);
            var hadCardsBefore = previousSuits.Count > 0;
            var suits = cards?
                .Select(card => NormalizeSuit(card?.Suit))
                .Where(suit => !string.IsNullOrWhiteSpace(suit))
                .ToList() ?? new List<string>();

            _resolvedSuits.Clear();
            _resolvedSuits.AddRange(suits);

            ReleaseCardSprites();
            DisableAllCards();

            var max = Mathf.Min(suits.Count, _cards.Length);
            for (var index = 0; index < max; index++)
            {
                var card = _cards[index];
                if (card == null)
                {
                    continue;
                }

                var handle = await LoadSpriteAsync(rank, suits[index], expectedVersion, token);
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

                var suit = suits[index];
                var isNewlyAdded = hadCardsBefore && !previousSuitSet.Contains(suit);
                if (isNewlyAdded)
                {
                    _pendingReceiveSuits.Add(suit);
                }

                var hiddenUntilAnimated = isNewlyAdded || _pendingReceiveSuits.Contains(suit);
                SetImageAlpha(card, hiddenUntilAnimated ? 0f : 1f);
            }

            var activeSuits = new HashSet<string>(suits, StringComparer.OrdinalIgnoreCase);
            _pendingReceiveSuits.RemoveWhere(suit => !activeSuits.Contains(suit));

            CacheCardPositions();
            ApplyTint(_button != null && _button.interactable ? Color.white : DisabledTint);

            if (_deferredReceiveBatches.Count > 0)
            {
                ProcessDeferredReceivesAsync(expectedVersion).Forget();
            }
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

        private void ReleaseCardSprites()
        {
            foreach (var handle in _cardSprites)
            {
                handle?.Dispose();
            }

            _cardSprites.Clear();
        }

        private void DisableAllCards()
        {
            if (_cards == null)
            {
                return;
            }

            foreach (var card in _cards)
            {
                if (card == null)
                {
                    continue;
                }

                card.sprite = null;
                card.enabled = false;
                card.gameObject.SetActive(false);
                SetImageAlpha(card, 1f);
            }
        }

        private void SetInteractable(bool canPress)
        {
            if (_button != null)
            {
                _button.interactable = canPress;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            ApplyTint(canPress ? Color.white : DisabledTint);
        }

        private void CacheCardPositions()
        {
            if (_cards == null)
            {
                return;
            }

            foreach (var card in _cards)
            {
                if (card == null)
                {
                    continue;
                }

                _cardBasePositions[card] = card.rectTransform.anchoredPosition;
            }
        }

        private void ApplyTint(Color color)
        {
            if (_cards == null)
            {
                return;
            }

            foreach (var card in _cards)
            {
                if (card == null)
                {
                    continue;
                }

                var current = card.color;
                card.color = new Color(color.r, color.g, color.b, current.a);
            }
        }

        private void ResetBindings()
        {
            _bindings?.Dispose();
            _bindings = null;
            _bindingsCts?.Cancel();
            _bindingsCts?.Dispose();
            _bindingsCts = null;
            _viewModel = null;
            _bindingVersion++;
            _resolvedSuits.Clear();
            _pendingReceiveSuits.Clear();
            _deferredReceiveBatches.Clear();
        }

        private void ResetAnimations()
        {
            _hasCompletedSetAnimation = false;
            _suppressRemovalAnimation = false;
            _externalAnimationCount = 0;
            _isAnimationRunning = false;

            if (_rectTransform != null)
            {
                DOTween.Kill(_rectTransform);
                _rectTransform.anchoredPosition = _baseAnchoredPosition;
            }

            if (_cards == null)
            {
                return;
            }

            foreach (var card in _cards)
            {
                if (card == null)
                {
                    continue;
                }

                DOTween.Kill(card);
                if (_cardBasePositions.TryGetValue(card, out var basePos))
                {
                    card.rectTransform.anchoredPosition = basePos;
                }

                SetImageAlpha(card, 1f);
            }
        }

        private UniTask EnqueueAnimation(Func<CancellationToken, UniTask> animation)
        {
            return RunQueued(animation);
        }

        private async UniTask RunQueued(Func<CancellationToken, UniTask> animation)
        {
            var token = _bindingsCts?.Token ?? CancellationToken.None;
            var lockTaken = false;
            try
            {
                await _animationGate.WaitAsync(token);
                lockTaken = true;
                _isAnimationRunning = true;
                await animation(token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (lockTaken)
                {
                    _isAnimationRunning = false;
                    _animationGate.Release();
                    NotifyAnimationsBecameIdleIfNeeded();
                }
            }
        }

        private async UniTask DrainReceiveQueueAsync()
        {
            if (_isDrainingReceiveQueue || _viewModel == null)
            {
                return;
            }

            _isDrainingReceiveQueue = true;
            try
            {
                while (_viewModel != null && _viewModel.TryDequeueCardsReceived(out var payload))
                {
                    await PlayReceivedBatchAsync(payload);
                }
            }
            finally
            {
                _isDrainingReceiveQueue = false;
            }
        }

        private async UniTask PlayReceivedBatchAsync(RankCardsReceivedEvent payload)
        {
            var expectedVersion = _bindingVersion;
            await EnqueueAnimation(token => PlayReceivedBatchCoreAsync(payload, expectedVersion, token));
        }

        private async UniTaskVoid ProcessDeferredReceivesAsync(int expectedVersion)
        {
            if (_deferredReceiveBatches.Count == 0)
            {
                return;
            }

            await EnqueueAnimation(token => FlushDeferredReceivesAsync(expectedVersion, token));
        }

        private async UniTask FlushDeferredReceivesAsync(int expectedVersion, CancellationToken token)
        {
            while (_deferredReceiveBatches.Count > 0)
            {
                if (token.IsCancellationRequested || expectedVersion != _bindingVersion)
                {
                    return;
                }

                var payload = _deferredReceiveBatches.Peek();
                if (!TryResolveReceiveSteps(payload.Suits, out var receiveSteps))
                {
                    return;
                }

                _deferredReceiveBatches.Dequeue();
                await PlayReceiveBatchAsync(payload, receiveSteps, expectedVersion, token);
            }
        }

        private async UniTask PlayReceivedBatchCoreAsync(
            RankCardsReceivedEvent payload,
            int expectedVersion,
            CancellationToken token)
        {
            if (token.IsCancellationRequested || expectedVersion != _bindingVersion)
            {
                return;
            }

            if (!TryResolveReceiveSteps(payload.Suits, out var receiveSteps))
            {
                _deferredReceiveBatches.Enqueue(CloneReceivePayload(payload));
                return;
            }

            await PlayReceiveBatchAsync(payload, receiveSteps, expectedVersion, token);
        }

        private async UniTask PlayReceiveBatchAsync(
            RankCardsReceivedEvent payload,
            IReadOnlyList<(int Index, string Suit)> receiveSteps,
            int expectedVersion,
            CancellationToken token)
        {
            if (token.IsCancellationRequested || expectedVersion != _bindingVersion)
            {
                return;
            }

            for (var i = 0; i < receiveSteps.Count; i++)
            {
                if (token.IsCancellationRequested || expectedVersion != _bindingVersion)
                {
                    return;
                }

                var step = receiveSteps[i];
                await PlayReceiveAnimationAsync(step.Index, payload.Source, step.Suit, token);
            }

            if (!payload.CompletedSet || _hasCompletedSetAnimation || token.IsCancellationRequested || expectedVersion != _bindingVersion)
            {
                return;
            }

            await PlaySetCompleteSequenceAsync(token);
            if (!token.IsCancellationRequested && expectedVersion == _bindingVersion)
            {
                _hasCompletedSetAnimation = true;
                SetCompletionAnimationFinished?.Invoke(this);
            }
        }

        private bool TryResolveReceiveSteps(
            IReadOnlyList<string> suits,
            out List<(int Index, string Suit)> receiveSteps)
        {
            receiveSteps = new List<(int Index, string Suit)>();
            if (suits == null || suits.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < suits.Count; i++)
            {
                var normalizedSuit = NormalizeSuit(suits[i]);
                if (string.IsNullOrWhiteSpace(normalizedSuit))
                {
                    continue;
                }

                if (!TryGetReadyCardIndex(normalizedSuit, out var index))
                {
                    return false;
                }

                receiveSteps.Add((index, normalizedSuit));
            }

            return true;
        }

        private static RankCardsReceivedEvent CloneReceivePayload(RankCardsReceivedEvent payload)
        {
            var suits = payload.Suits != null
                ? payload.Suits.ToArray()
                : Array.Empty<string>();

            return new RankCardsReceivedEvent(
                payload.Source,
                suits,
                payload.EventSeq,
                payload.CompletedSet);
        }

        private bool TryGetReadyCardIndex(string normalizedSuit, out int index)
        {
            index = -1;
            if (!string.IsNullOrWhiteSpace(normalizedSuit))
            {
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

            return TryGetLastReadyCardIndex(out index);
        }

        private bool TryGetLastReadyCardIndex(out int index)
        {
            index = -1;
            if (_cards == null || _cards.Length == 0)
            {
                return false;
            }

            for (var i = _cards.Length - 1; i >= 0; i--)
            {
                if (!IsCardReady(i))
                {
                    continue;
                }

                index = i;
                return true;
            }

            return false;
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

        private bool IsCardReady(int cardIndex)
        {
            if (_cards == null || cardIndex < 0 || cardIndex >= _cards.Length)
            {
                return false;
            }

            var card = _cards[cardIndex];
            return card != null && card.gameObject.activeSelf && card.sprite != null;
        }

        private async UniTask PlayReceiveAnimationAsync(int cardIndex, string source, string suit, CancellationToken token)
        {
            if (token.IsCancellationRequested || !IsCardReady(cardIndex))
            {
                return;
            }

            var card = _cards[cardIndex];
            var rect = card.rectTransform;
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            CacheCardPositions();

            var basePos = _cardBasePositions.TryGetValue(card, out var cached) ? cached : rect.anchoredPosition;
            var targetWorld = rect.position;
            var receiveOrigin = ResolveReceiveOrigin(source);

            DOTween.Kill(card);
            DOTween.Kill(rect);
            if (receiveOrigin != null)
            {
                rect.position = receiveOrigin.position;
            }
            else
            {
                rect.anchoredPosition = basePos + ResolveReceiveOffset(source);
            }

            SetImageAlpha(card, 0f);

            var sequence = DOTween.Sequence().SetUpdate(true);
            if (receiveOrigin != null)
            {
                sequence.Append(rect.DOMove(targetWorld, _receiveDuration).SetEase(Ease.OutQuad));
            }
            else
            {
                sequence.Append(rect.DOAnchorPos(basePos, _receiveDuration).SetEase(Ease.OutQuad));
            }

            sequence.Join(card.DOFade(1f, _receiveDuration));
            await TweenAwaiter.AwaitAsync(sequence, _receiveDuration + _tweenTimeoutBuffer, token);
            rect.anchoredPosition = basePos;
            SetImageAlpha(card, 1f);
            if (!string.IsNullOrWhiteSpace(suit))
            {
                _pendingReceiveSuits.Remove(suit);
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

        private async UniTask PlaySetCompleteSequenceAsync(CancellationToken token)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(_canvasGroup.DOFade(0.3f, _setCompleteFlashDuration));
            sequence.Append(_canvasGroup.DOFade(1f, _setCompleteFlashDuration));
            sequence.Append(_canvasGroup.DOFade(0f, _setCompleteFadeDuration));
            await TweenAwaiter.AwaitAsync(
                sequence,
                (_setCompleteFlashDuration * 2f) + _setCompleteFadeDuration + _tweenTimeoutBuffer,
                token);
        }

        private async UniTask PlayTransferRemovalSequenceAsync(CancellationToken token)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            if (_rectTransform != null)
            {
                var shake = _rectTransform
                    .DOShakeAnchorPos(_transferShakeDuration, _transferShakeStrength, 10, 0f)
                    .SetUpdate(true);
                await TweenAwaiter.AwaitAsync(shake, _transferShakeDuration + _tweenTimeoutBuffer, token);
            }

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(_canvasGroup.DOFade(0f, _transferFadeDuration));
            if (_rectTransform != null)
            {
                var start = _rectTransform.anchoredPosition;
                sequence.Join(_rectTransform.DOAnchorPos(start + new Vector2(0f, _receiveOffsetY), _transferFadeDuration));
            }

            await TweenAwaiter.AwaitAsync(sequence, _transferFadeDuration + _tweenTimeoutBuffer, token);
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            image.color = new Color(color.r, color.g, color.b, alpha);
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank) ? string.Empty : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit) ? string.Empty : suit.Trim().ToLowerInvariant();
        }

        private void EndExternalAnimation()
        {
            if (_externalAnimationCount <= 0)
            {
                return;
            }

            _externalAnimationCount--;
            NotifyAnimationsBecameIdleIfNeeded();
        }

        private void NotifyAnimationsBecameIdleIfNeeded()
        {
            if (!HasActiveAnimations)
            {
                AnimationsBecameIdle?.Invoke(this);
            }
        }
    }
}
