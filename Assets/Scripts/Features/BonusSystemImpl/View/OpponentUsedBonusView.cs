using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.BonusSystemImpl.Presenters;
using Features.BonusSystemImpl.ViewModel;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.BonusSystemImpl.View
{
    public class OpponentUsedBonusView : MonoBehaviour
    {
        private const string BonusSpriteFormat = "Art/Cards/Bonus/bonus_{0}";

        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _cardRoot;
        [SerializeField] private Image _cardImage;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _enterOffset = 40f;
        [SerializeField] private float _enterDuration = 0.2f;
        [SerializeField] private float _exitDuration = 0.2f;

        private OpponentUsedBonusPresenter _presenter;
        private AssetService _assetService;
        private CompositeDisposable _subscriptions = new();

        private ManagedAsset<Sprite> _iconHandle;
        private Sequence _sequence;
        private CancellationTokenSource _cts;
        private Vector2 _basePosition;

        [Inject]
        public void Construct(OpponentUsedBonusPresenter presenter, AssetService assetService)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        }

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_cardRoot == null)
            {
                _cardRoot = GetComponent<RectTransform>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_cardRoot != null)
            {
                _basePosition = _cardRoot.anchoredPosition;
            }

            SetVisible(false);
        }

        private void OnEnable()
        {
            _subscriptions = new CompositeDisposable();
            _presenter.State
                .Subscribe(ApplyState)
                .AddTo(_subscriptions);
        }

        private void OnDisable()
        {
            _subscriptions.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _sequence?.Kill();
            ReleaseIcon();
            SetVisible(false);
        }

        private void ApplyState(OpponentUsedBonusViewState state)
        {
            if (!state.IsVisible || string.IsNullOrWhiteSpace(state.BonusType))
            {
                Hide();
                return;
            }

            ShowInternal(state.BonusType, state.HideDelay);
        }

        private void ShowInternal(string bonusType, float? hideDelay)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            ShowAsync(bonusType, hideDelay, _cts.Token).Forget();
        }

        private async UniTaskVoid ShowAsync(string bonusType, float? hideDelay, CancellationToken token)
        {
            await UpdateIconAsync(bonusType, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _sequence?.Kill();
            SetVisible(true);

            if (_cardRoot != null)
            {
                _cardRoot.anchoredPosition = _basePosition + new Vector2(0f, _enterOffset);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            var sequence = DOTween.Sequence();
            if (_cardRoot != null)
            {
                sequence.Join(_cardRoot.DOAnchorPos(_basePosition, _enterDuration).SetEase(Ease.OutQuad));
            }

            if (_canvasGroup != null)
            {
                sequence.Join(_canvasGroup.DOFade(1f, _enterDuration));
            }

            if (hideDelay.HasValue)
            {
                sequence.AppendInterval(Mathf.Max(0f, hideDelay.Value));
                sequence.AppendCallback(Hide);
            }

            _sequence = sequence;
        }

        private void Hide()
        {
            _sequence?.Kill();

            var sequence = DOTween.Sequence();
            if (_canvasGroup != null)
            {
                sequence.Append(_canvasGroup.DOFade(0f, _exitDuration));
            }

            if (_cardRoot != null)
            {
                sequence.Join(_cardRoot.DOAnchorPos(_basePosition + new Vector2(0f, _enterOffset), _exitDuration)
                    .SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() => SetVisible(false));
            _sequence = sequence;
        }

        private async UniTask UpdateIconAsync(string bonusType, CancellationToken token)
        {
            if (_cardImage == null || _assetService == null)
            {
                return;
            }

            var normalized = string.IsNullOrWhiteSpace(bonusType) ? string.Empty : bonusType.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                _cardImage.enabled = false;
                ReleaseIcon();
                return;
            }

            var assetId = string.Format(BonusSpriteFormat, normalized);
            ReleaseIcon();

            try
            {
                var handle = await _assetService.Get<Sprite>(assetId);
                if (token.IsCancellationRequested)
                {
                    handle.Dispose();
                    return;
                }

                _iconHandle = handle;
                _cardImage.sprite = _iconHandle.Asset;
                _cardImage.enabled = _cardImage.sprite != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpponentBonus] Failed to load bonus icon '{assetId}': {ex.Message}");
                _cardImage.enabled = false;
            }
        }

        private void ReleaseIcon()
        {
            if (_iconHandle != null)
            {
                _iconHandle.Dispose();
                _iconHandle = null;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.SetActive(visible);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
