using System;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.ViewModel;
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
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;

        private AssetService _assetService;
        private BonusCardViewModel _viewModel;
        private ManagedAsset<Sprite> _iconHandle;
        private CompositeDisposable _bindings;

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

        private void ResetBindings()
        {
            _bindings?.Dispose();
            _bindings = null;
            _viewModel = null;
        }
    }
}
