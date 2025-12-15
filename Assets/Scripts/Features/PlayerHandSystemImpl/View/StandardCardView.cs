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
    public class StandardCardView : MonoBehaviour
    {
        private const string SpriteIdFormat = "card_{0}_{1}";

        [SerializeField] private GameObject[] _underCards;
        [SerializeField] private Image _cardImage;
        [SerializeField] private Button _button;

        private AssetService _assetService;
        private StandardCardViewModel _viewModel;
        private CompositeDisposable _bindings;
        private ManagedAsset<Sprite> _activeSprite;
        private string _currentSuit = string.Empty;

        [Inject]
        public void Construct(AssetService assetService)
        {
            _assetService = assetService;
        }

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>() ?? _cardImage?.GetComponent<Button>();
            }
        }

        private void OnDestroy()
        {
            ResetBindings();
            ReleaseSprite();
        }

        public async UniTask Initialize(StandardCardViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            ResetBindings();
            _viewModel = viewModel;
            _bindings = new CompositeDisposable();

            UpdateStack(viewModel.Amount.CurrentValue);
            await UpdateSpriteAsync(viewModel.LastSuit.CurrentValue);

            viewModel.LastSuit
                .Subscribe(suit => UpdateSpriteAsync(suit).Forget())
                .AddTo(_bindings);

            viewModel.Amount
                .Subscribe(UpdateStack)
                .AddTo(_bindings);

            viewModel.CanPress
                .Subscribe(SetInteractable)
                .AddTo(_bindings);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _viewModel?.Press());
            }
        }

        private void UpdateStack(int amount)
        {
            if (_underCards == null || _underCards.Length == 0)
            {
                return;
            }

            var visible = Mathf.Max(0, amount - 1);
            for (var index = 0; index < _underCards.Length; index++)
            {
                var card = _underCards[index];
                if (card == null) continue;
                card.SetActive(index < visible);
            }
        }

        private void SetInteractable(bool canPress)
        {
            if (_button != null)
            {
                _button.interactable = canPress;
            }
            else if (_cardImage != null)
            {
                _cardImage.raycastTarget = canPress;
            }
        }

        private async UniTask UpdateSpriteAsync(string suit)
        {
            if (_cardImage == null || _assetService == null || _viewModel == null)
            {
                return;
            }

            var normalizedSuit = string.IsNullOrWhiteSpace(suit) ? string.Empty : suit.Trim().ToLowerInvariant();
            if (string.Equals(_currentSuit, normalizedSuit, StringComparison.Ordinal))
            {
                return;
            }

            _currentSuit = normalizedSuit;

            if (string.IsNullOrEmpty(_viewModel.Rank) || string.IsNullOrEmpty(normalizedSuit))
            {
                _cardImage.enabled = false;
                ReleaseSprite();
                return;
            }

            var spriteId = string.Format(SpriteIdFormat, _viewModel.Rank, normalizedSuit);
            ReleaseSprite();

            try
            {
                _activeSprite = await _assetService.Get<Sprite>(spriteId);
                _cardImage.sprite = _activeSprite.Asset;
                _cardImage.enabled = _cardImage.sprite != null;
            }
            catch (Exception ex)
            {
                Debug.unityLogger.LogWarning("PlayerHand", $"Failed to load card sprite '{spriteId}': {ex.Message}");
                _cardImage.enabled = false;
            }
        }

        private void ReleaseSprite()
        {
            if (_activeSprite != null)
            {
                _activeSprite.Dispose();
                _activeSprite = null;
            }
        }

        public void ResetView()
        {
            ResetBindings();
            ReleaseSprite();
            _currentSuit = string.Empty;
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.interactable = false;
            }
            if (_cardImage != null)
            {
                _cardImage.sprite = null;
                _cardImage.enabled = false;
            }
            UpdateStack(0);
        }

        private void ResetBindings()
        {
            _bindings?.Dispose();
            _bindings = null;
            _viewModel = null;
        }
    }
}
