using System;
using System.Collections.Generic;
using System.Linq;
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
    public class StandardCardView : MonoBehaviour
    {
        private const string SpriteIdFormat = "Art/Cards/Standard/card_{0}_{1}";
        private const float ReceiveOffsetY = 24f;
        private const float ReceiveDuration = 0.18f;
        private const float TransferShakeDuration = 0.12f;
        private const float TransferShakeStrength = 6f;
        private const float TransferFadeDuration = 0.2f;
        private const float SetCompleteFlashDuration = 0.08f;
        private const float SetCompleteFadeDuration = 0.2f;

        [SerializeField] private Image[] _cards;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;

        private AssetService _assetService;
        private StandardCardViewModel _viewModel;
        private CompositeDisposable _bindings;
        private readonly List<ManagedAsset<Sprite>> _cardSprites = new();
        private readonly Dictionary<Image, Vector2> _cardBasePositions = new();
        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private UniTask _animationChain = UniTask.CompletedTask;
        private int _lastCount;
        private bool _pendingSetComplete;

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

        public async UniTask Initialize(StandardCardViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            ResetBindings();
            _viewModel = viewModel;
            _bindings = new CompositeDisposable();

            await UpdateCardsAsync(viewModel.Suits.CurrentValue);

            viewModel.Suits
                .Subscribe(suits => UpdateCardsAsync(suits).Forget())
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
                _canvasGroup.alpha = canPress ? 1f : 0.45f;
            }
        }

        private async UniTask UpdateCardsAsync(IReadOnlyList<string> suits)
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

            var normalized = suits?
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
                EnqueueAnimation(() => PlayReceiveAnimationAsync(newCount - 1));
            }
            if (newCount == 0 && _pendingSetComplete)
            {
                _pendingSetComplete = false;
                EnqueueAnimation(PlaySetCompleteAnimationAsync);
            }
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

                if (!_cardBasePositions.ContainsKey(card))
                {
                    _cardBasePositions[card] = card.rectTransform.anchoredPosition;
                }
            }
        }

        private void ResetAnimations()
        {
            _animationChain = UniTask.CompletedTask;
            _pendingSetComplete = false;
            _lastCount = 0;

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
            _animationChain = _animationChain.ContinueWith(animation);
            return _animationChain;
        }

        public UniTask PlaySetCompleteAnimationAsync()
        {
            return EnqueueAnimation(PlaySetCompleteSequenceAsync);
        }

        public UniTask PlayTransferRemovalAnimationAsync()
        {
            return EnqueueAnimation(PlayTransferRemovalSequenceAsync);
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
    }
}
