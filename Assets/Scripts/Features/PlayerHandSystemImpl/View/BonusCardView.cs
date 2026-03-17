using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.BonusSystemImpl.View;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Features.PlayerHandSystemImpl.Utils;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.PlayerHandSystemImpl.View
{
    public class BonusCardView : MonoBehaviour
    {
        private const string BonusSpriteFormat = "Art/Cards/Bonus/bonus_{0}";

        [SerializeField] private Image _icon;
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private CanvasGroup _visualCanvasGroup;
        [SerializeField] private Button _button;
        [SerializeField] private Button _infoButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [Header("Animation")]
        [SerializeField] private float _receiveOffsetY = -32f;
        [SerializeField, Min(0.01f)] private float _receiveDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float _tweenTimeoutPadding = 0.35f;

        private AssetService _assetService;
        private WindowSystem _windowSystem;
        private BonusCardViewModel _viewModel;
        private ManagedAsset<Sprite> _iconHandle;
        private CompositeDisposable _bindings;
        private RectTransform _iconRect;
        private RectTransform _visualRect;
        private Vector2 _visualBaseAnchored;

        [Inject]
        public void Construct(AssetService assetService, WindowSystem windowSystem)
        {
            _assetService = assetService;
            _windowSystem = windowSystem;
        }

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_visualRoot == null)
            {
                _visualRoot = transform as RectTransform;
            }

            if (_visualRoot != null)
            {
                _visualRect = _visualRoot;
                _visualBaseAnchored = _visualRect.anchoredPosition;
                if (_visualCanvasGroup == null)
                {
                    _visualCanvasGroup = _visualRoot.GetComponent<CanvasGroup>();
                }
            }

            if (_icon != null)
            {
                _iconRect = _icon.rectTransform;
            }
        }

        private void OnDestroy()
        {
            ResetBindings();
            ReleaseIcon();
        }

        public async UniTask Initialize(BonusCardViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            ResetBindings();
            _viewModel = viewModel;
            _bindings = new CompositeDisposable();

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _viewModel?.TriggerUse());
            }

            if (_infoButton != null)
            {
                _infoButton.onClick.RemoveAllListeners();
                _infoButton.onClick.AddListener(OnInfoClicked);
            }

            await UpdateIconAsync(_viewModel.BonusCardType);
            viewModel.CanUse
                .Subscribe(UpdateState)
                .AddTo(_bindings);
        }

        private void UpdateState(bool canUse)
        {
            if (_button != null)
            {
                _button.interactable = canUse;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = canUse ? 1f : 0.45f;
            }
        }

        private async UniTask UpdateIconAsync(string bonusType)
        {
            if (_icon == null || _assetService == null)
            {
                return;
            }

            var normalized = string.IsNullOrWhiteSpace(bonusType) ? string.Empty : bonusType.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                _icon.enabled = false;
                ReleaseIcon();
                return;
            }

            var assetId = string.Format(BonusSpriteFormat, normalized);
            ReleaseIcon();

            try
            {
                _iconHandle = await _assetService.Get<Sprite>(assetId);
                _icon.sprite = _iconHandle.Asset;
                _icon.enabled = _icon.sprite != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed to load bonus icon '{assetId}': {ex.Message}");
                _icon.enabled = false;
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

        public void ResetView()
        {
            ResetBindings();
            ReleaseIcon();
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = false;
            }

            if (_infoButton != null)
            {
                _infoButton.onClick.RemoveAllListeners();
            }

            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        public void SetTint(Color color)
        {
            if (_icon != null)
            {
                _icon.color = color;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        private void ResetBindings()
        {
            _bindings?.Dispose();
            _bindings = null;
            _viewModel = null;
        }

        public string BonusType => _viewModel?.BonusCardType ?? string.Empty;

        public async UniTask PlayReceiveFromAsync(RectTransform source)
        {
            if (_visualRect == null || source == null)
            {
                return;
            }

            if (_icon == null)
            {
                return;
            }

            if (_visualCanvasGroup == null)
            {
                _visualCanvasGroup = _canvasGroup;
            }

            var baseAlpha = _visualCanvasGroup != null ? _visualCanvasGroup.alpha : 1f;
            if (_visualCanvasGroup != null)
            {
                _visualCanvasGroup.alpha = 0f;
            }

            for (var i = 0; i < 12 && (_icon.sprite == null || !_icon.enabled); i++)
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(50));
            }

            if (_icon.sprite == null)
            {
                if (_visualCanvasGroup != null)
                {
                    _visualCanvasGroup.alpha = baseAlpha;
                }
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            DOTween.Kill(_visualRect);
            _visualBaseAnchored = _visualRect.anchoredPosition;
            var targetWorld = _visualRect.position;
            _visualRect.position = source.position + new Vector3(0f, _receiveOffsetY, 0f);

            _icon.enabled = true;

            var sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(_visualRect.DOMove(targetWorld, _receiveDuration).SetEase(Ease.OutQuad));
            if (_visualCanvasGroup != null)
            {
                sequence.Join(_visualCanvasGroup.DOFade(baseAlpha, _receiveDuration));
            }
            await TweenAwaiter.AwaitAsync(sequence, _receiveDuration + _tweenTimeoutPadding);
            _visualRect.anchoredPosition = _visualBaseAnchored;
            if (_visualCanvasGroup != null)
            {
                _visualCanvasGroup.alpha = baseAlpha;
            }
        }

        private void OnInfoClicked()
        {
            if (_windowSystem == null || _viewModel == null)
            {
                return;
            }

            var title = _viewModel.InfoTitle;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = _viewModel.BonusCardType;
            }

            var description = _viewModel.InfoText;
            _windowSystem
                .ShowWindowAsync<BonusCardInfoContent, BonusCardInfoContentData>(
                    nameof(ModalWindowTemplate),
                    new BonusCardInfoContentData(_icon != null ? _icon.sprite : null, title, description))
                .Forget();
        }
    }
}
