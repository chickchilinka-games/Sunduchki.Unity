using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.PlayerHandSystemImpl.View
{
    public class RankStackView : MonoBehaviour
    {
        private const string SpriteIdFormat = "Art/Cards/Standard/card_{0}_{1}";
        private const float ReceiveOffsetY = 24f;
        private const float ReceiveDuration = 0.28f;
        private const float ReceivePreFadeDuration = 0.12f;
        private const float ReceivePreFadeAlpha = 0.85f;
        private const float DeckReceiveOffsetY = -32f;
        private const float TransferShakeDuration = 0.12f;
        private const float TransferShakeStrength = 6f;
        private const float TransferFadeDuration = 0.2f;
        private const float SetCompleteFlashDuration = 0.08f;
        private const float SetCompleteFadeDuration = 0.2f;
        private static readonly Color DisabledTint = new(0.65f, 0.65f, 0.65f, 1f);

        [SerializeField] private Image[] _cards;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;

        private AssetService _assetService;
        private RankStackViewModel _viewModel;
        private CompositeDisposable _bindings;
        private readonly List<ManagedAsset<Sprite>> _cardSprites = new();
        private readonly Dictionary<Image, Vector2> _cardBasePositions = new();
        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private readonly SemaphoreSlim _animationGate = new(1, 1);
        private int _lastCount;
        private bool _pendingSetComplete;
        private int _externalAnimationCount;
        private bool _suppressRemovalAnimation;

        public bool HasPendingSetComplete => _pendingSetComplete;

        [Inject]
        public void Construct(AssetService assetService)
        {
            _assetService = assetService;
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
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            ResetBindings();
            _viewModel = viewModel;
            _bindings = new CompositeDisposable();

            await UpdateCardsAsync(viewModel.Cards.CurrentValue);

            viewModel.Cards
                .Subscribe(cards => UpdateCardsAsync(cards).Forget())
                .AddTo(_bindings);

            viewModel.CanPress
                .Subscribe(SetInteractable)
                .AddTo(_bindings);

            viewModel.SetCompleted
                .Subscribe(_ => _pendingSetComplete = true)
                .AddTo(_bindings);

            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _viewModel?.Press());
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

        private async UniTask UpdateCardsAsync(IReadOnlyList<StandardCardItemViewModel> cards)
        {
            if (_cards == null || _cards.Length == 0 || _assetService == null || _viewModel == null)
            {
                return;
            }

            var rank = _viewModel.Rank;
            if (string.IsNullOrWhiteSpace(rank))
            {
                DisableAllCards();
                ReleaseCardSprites();
                _lastCount = 0;
                return;
            }

            var normalized = cards?
                .Select(card => card?.Suit)
                .Where(suit => !string.IsNullOrWhiteSpace(suit))
                .Select(suit => suit.Trim().ToLowerInvariant())
                .ToList() ?? new List<string>();

            ReleaseCardSprites();

            CacheCardPositions();

            for (var index = 0; index < _cards.Length; index++)
            {
                var card = _cards[index];
                if (card == null)
                {
                    continue;
                }

                if (index >= normalized.Count)
                {
                    card.sprite = null;
                    card.enabled = false;
                    card.gameObject.SetActive(false);
                    continue;
                }

                var spriteId = string.Format(SpriteIdFormat, rank, normalized[index]);
                try
                {
                    var handle = await _assetService.Get<Sprite>(spriteId);
                    _cardSprites.Add(handle);
                    card.sprite = handle.Asset;
                    card.enabled = card.sprite != null;
                    card.gameObject.SetActive(card.sprite != null);
                    if (card.sprite != null)
                    {
                        var current = card.color;
                        card.color = new Color(current.r, current.g, current.b, 1f);
                    }
                }
                catch (Exception ex)
                {
                    Debug.unityLogger.LogWarning("PlayerHand",
                        $"Failed to load card sprite '{spriteId}': {ex.Message}");
                    card.sprite = null;
                    card.enabled = false;
                    card.gameObject.SetActive(false);
                }
            }

            var newCount = normalized.Count;
            var oldCount = _lastCount;
            _lastCount = newCount;

            if (newCount > oldCount && newCount > 0)
            {
                // Receive animation is driven externally (deck draw / transfer), keep view static here.
            }
            if (newCount == 0 && _pendingSetComplete)
            {
                _pendingSetComplete = false;
                PlaySetCompleteAnimationAsync().Forget();
            }

            ApplyTint(_button != null && _button.interactable ? Color.white : DisabledTint);
        }

        private void ReleaseCardSprites()
        {
            foreach (var handle in _cardSprites)
            {
                handle?.Dispose();
            }

            _cardSprites.Clear();
        }

        private void ResetUnderCards()
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
                card.gameObject.SetActive(false);
            }
        }

        public void ResetView()
        {
            ResetBindings();
            ReleaseCardSprites();
            ResetAnimations();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = false;
            }

            DisableAllCards();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            ResetUnderCards();
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
            }
        }

        private void ResetBindings()
        {
            _bindings?.Dispose();
            _bindings = null;
            _viewModel = null;
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

        private void ResetAnimations()
        {
            _pendingSetComplete = false;
            _lastCount = 0;
            _externalAnimationCount = 0;
            _suppressRemovalAnimation = false;

            if (_rectTransform != null)
            {
                DOTween.Kill(_rectTransform);
                _rectTransform.anchoredPosition = _baseAnchoredPosition;
            }

            if (_cards != null)
            {
                foreach (var card in _cards)
                {
                    if (card == null)
                    {
                        continue;
                    }

                    DOTween.Kill(card);
                    if (_cardBasePositions.TryGetValue(card, out var pos))
                    {
                        card.rectTransform.anchoredPosition = pos;
                    }

                    var color = card.color;
                    card.color = new Color(color.r, color.g, color.b, 1f);
                }
            }
        }

        private UniTask EnqueueAnimation(Func<UniTask> animation)
        {
            return RunQueued(animation);
        }

        private async UniTask RunQueued(Func<UniTask> animation)
        {
            await _animationGate.WaitAsync();
            try
            {
                await animation();
            }
            finally
            {
                _animationGate.Release();
            }
        }

        public UniTask PlaySetCompleteAnimationAsync()
        {
            return EnqueueAnimation(PlaySetCompleteSequenceAsync);
        }

        public UniTask PlayTransferRemovalAnimationAsync()
        {
            return EnqueueAnimation(PlayTransferRemovalSequenceAsync);
        }

        public bool SkipRemovalAnimation => _suppressRemovalAnimation;

        public void MarkTransferredOut()
        {
            _suppressRemovalAnimation = true;
        }

        public IDisposable BeginExternalAnimation()
        {
            _externalAnimationCount++;
            return new ExternalAnimationScope(this);
        }

        public UniTask WaitForExternalAnimationsAsync()
        {
            return _externalAnimationCount == 0
                ? UniTask.CompletedTask
                : UniTask.WaitUntil(() => _externalAnimationCount == 0);
        }

        private void EndExternalAnimation()
        {
            if (_externalAnimationCount <= 0)
            {
                return;
            }

            _externalAnimationCount--;
        }

        public bool TryGetCardImage(string suit, out Image image)
        {
            image = null;
            if (_cards == null || _cards.Length == 0)
            {
                return false;
            }

            var index = ResolveCardIndex(suit);
            if (index < 0 || index >= _cards.Length)
            {
                return false;
            }

            var candidate = _cards[index];
            if (candidate == null || candidate.sprite == null || !candidate.gameObject.activeSelf)
            {
                return false;
            }

            image = candidate;
            return true;
        }

        public void SetCardVisibility(Image image, bool visible)
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            image.color = new Color(color.r, color.g, color.b, visible ? 1f : 0f);
        }

        public void SetTint(Color color)
        {
            ApplyTint(color);
        }

        public void PrepareReceive(string suit)
        {
            if (_cards == null || _cards.Length == 0)
            {
                return;
            }

            var cardIndex = ResolveCardIndex(suit);
            if (cardIndex < 0 || cardIndex >= _cards.Length)
            {
                return;
            }

            var card = _cards[cardIndex];
            if (card == null)
            {
                return;
            }

            if (!card.gameObject.activeSelf)
            {
                card.gameObject.SetActive(true);
            }

            var color = card.color;
            card.color = new Color(color.r, color.g, color.b, 0f);
        }

        public UniTask PlayReceiveFromAsync(RectTransform source, string suit)
        {
            if (source == null || _cards == null || _cards.Length == 0)
            {
                return UniTask.CompletedTask;
            }

            var cardIndex = ResolveCardIndex(suit);
            return EnqueueAnimation(() => PlayReceiveFromSourceAsync(source, cardIndex));
        }

        private async UniTask PlayReceiveAnimationAsync(int cardIndex)
        {
            if (_cards == null || cardIndex < 0 || cardIndex >= _cards.Length)
            {
                return;
            }

            var card = _cards[cardIndex];
            if (card == null || !card.gameObject.activeSelf)
            {
                return;
            }

            var rect = card.rectTransform;
            var basePos = _cardBasePositions.TryGetValue(card, out var cached) ? cached : rect.anchoredPosition;
            rect.anchoredPosition = basePos + new Vector2(0f, ReceiveOffsetY);

            var color = card.color;
            card.color = new Color(color.r, color.g, color.b, 0f);

            var sequence = DOTween.Sequence();
            sequence.Append(rect.DOAnchorPos(basePos, ReceiveDuration).SetEase(Ease.OutQuad));
            sequence.Join(card.DOFade(1f, ReceiveDuration));
            await sequence.AsyncWaitForCompletion();
        }

        private async UniTask PlayReceiveFromSourceAsync(RectTransform source, int cardIndex)
        {
            if (_cards == null || cardIndex < 0 || cardIndex >= _cards.Length)
            {
                return;
            }

            var card = _cards[cardIndex];
            if (card == null)
            {
                return;
            }

            if (!await WaitForCardReadyAsync(cardIndex))
            {
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            CacheCardPositions();

            var rect = card.rectTransform;
            var basePos = _cardBasePositions.TryGetValue(card, out var cached) ? cached : rect.anchoredPosition;
            rect.anchoredPosition = basePos;
            var targetWorld = rect.position;
            var sourceWorld = source.position + new Vector3(0f, DeckReceiveOffsetY, 0f);

            DOTween.Kill(rect);
            rect.position = sourceWorld;

            var color = card.color;
            card.color = new Color(color.r, color.g, color.b, 0f);

            var sequence = DOTween.Sequence();
            sequence.Append(card.DOFade(ReceivePreFadeAlpha, ReceivePreFadeDuration));
            sequence.Append(rect.DOMove(targetWorld, ReceiveDuration).SetEase(Ease.OutQuad));
            sequence.Join(card.DOFade(1f, ReceiveDuration));
            await sequence.AsyncWaitForCompletion();
            rect.anchoredPosition = basePos;
        }

        private async UniTask<bool> WaitForCardReadyAsync(int cardIndex)
        {
            if (_cards == null || cardIndex < 0 || cardIndex >= _cards.Length)
            {
                return false;
            }

            for (var i = 0; i < 20; i++)
            {
                var card = _cards[cardIndex];
                if (card != null && card.gameObject.activeSelf && card.sprite != null)
                {
                    return true;
                }

                await UniTask.Delay(TimeSpan.FromMilliseconds(50));
            }

            return false;
        }

        private int ResolveCardIndex(string suit)
        {
            if (_cards == null || _cards.Length == 0)
            {
                return 0;
            }

            if (_viewModel == null)
            {
                return Mathf.Clamp(_lastCount - 1, 0, _cards.Length - 1);
            }

            var normalized = string.IsNullOrWhiteSpace(suit) ? string.Empty : suit.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                var suits = _viewModel.Cards.CurrentValue;
                for (var index = 0; index < suits.Count && index < _cards.Length; index++)
                {
                    var suitValue = suits[index]?.Suit;
                    if (string.Equals(suitValue, normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return index;
                    }
                }
            }

            var fallback = _viewModel.Cards.CurrentValue.Count - 1;
            return Mathf.Clamp(fallback, 0, _cards.Length - 1);
        }

        private async UniTask PlaySetCompleteSequenceAsync()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _pendingSetComplete = false;

            var sequence = DOTween.Sequence();
            sequence.Append(_canvasGroup.DOFade(0.3f, SetCompleteFlashDuration));
            sequence.Append(_canvasGroup.DOFade(1f, SetCompleteFlashDuration));
            sequence.Append(_canvasGroup.DOFade(0f, SetCompleteFadeDuration));
            await sequence.AsyncWaitForCompletion();
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

        private async UniTask PlayTransferRemovalSequenceAsync()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            if (_rectTransform != null)
            {
                await _rectTransform
                    .DOShakeAnchorPos(TransferShakeDuration, TransferShakeStrength, 10, 0f)
                    .AsyncWaitForCompletion();
            }

            var sequence = DOTween.Sequence();
            sequence.Append(_canvasGroup.DOFade(0f, TransferFadeDuration));
            if (_rectTransform != null)
            {
                var start = _rectTransform.anchoredPosition;
                sequence.Join(_rectTransform.DOAnchorPos(start + new Vector2(0f, ReceiveOffsetY), TransferFadeDuration));
            }
            await sequence.AsyncWaitForCompletion();
        }

        private sealed class ExternalAnimationScope : IDisposable
        {
            private RankStackView _owner;

            public ExternalAnimationScope(RankStackView owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                _owner.EndExternalAnimation();
                _owner = null;
            }
        }
    }
}
