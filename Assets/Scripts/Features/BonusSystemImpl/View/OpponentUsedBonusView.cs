using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Services;
using Modules.BonusSystem.Config;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.Lobby.Services;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Services;
using Modules.TurnSystem.Services;
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
        [SerializeField] private string[] _endOfTurnAttackBonuses =
        {
            "AskTwice",
            "StealExtraOnSuccess",
            "SilentAsk"
        };
        [SerializeField] private float _instantHideDelay = 0.6f;
        [SerializeField] private float _defenseHideDelay = 0.6f;

        private PlayerHandService _handService;
        private LobbyService _lobbyService;
        private TurnSequenceService _turnService;
        private ICardRequestService _cardRequestService;
        private IBonusCardRulesProvider _rulesProvider;
        private AssetService _assetService;

        private readonly HashSet<string> _defenseBonuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _endOfTurnBonuses = new(StringComparer.OrdinalIgnoreCase);
        private CompositeDisposable _subscriptions = new();

        private ManagedAsset<Sprite> _iconHandle;
        private Sequence _sequence;
        private CancellationTokenSource _cts;
        private Vector2 _basePosition;
        private bool _holdUntilTurnEnd;
        private string _holderPlayerId = string.Empty;

        [Inject]
        public void Construct(
            PlayerHandService handService,
            LobbyService lobbyService,
            TurnSequenceService turnService,
            ICardRequestService cardRequestService,
            IBonusCardRulesProvider rulesProvider,
            AssetService assetService)
        {
            _handService = handService ?? throw new ArgumentNullException(nameof(handService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _rulesProvider = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
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

            CacheRules();
            SetVisible(false);
        }

        private void OnEnable()
        {
            _subscriptions = new CompositeDisposable();
            _handService.BonusUsed
                .Subscribe(OnBonusUsed)
                .AddTo(_subscriptions);

            _turnService.State
                .Subscribe(state => OnTurnChanged(state.CurrentPlayerId))
                .AddTo(_subscriptions);

            _cardRequestService.State
                .Subscribe(OnCardRequestState)
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

        private void CacheRules()
        {
            _defenseBonuses.Clear();
            var defense = _rulesProvider.GetDefenseBonusTypes() ?? Array.Empty<string>();
            foreach (var type in defense)
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    _defenseBonuses.Add(type.Trim());
                }
            }

            _endOfTurnBonuses.Clear();
            foreach (var type in _endOfTurnAttackBonuses ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(type))
                {
                    _endOfTurnBonuses.Add(type.Trim());
                }
            }
        }

        private void OnBonusUsed(BonusCardUsageEvent payload)
        {
            if (string.IsNullOrWhiteSpace(payload.BonusType))
            {
                return;
            }

            var localId = _lobbyService.StateContext?.Data.PlayerId;
            var isLocal = !string.IsNullOrWhiteSpace(localId) &&
                          string.Equals(payload.PlayerId, localId, StringComparison.Ordinal);
            if (isLocal)
            {
                return;
            }

            var normalized = payload.BonusType.Trim();
            if (_defenseBonuses.Contains(normalized))
            {
                ShowTimed(normalized, _defenseHideDelay, payload.PlayerId);
                return;
            }

            if (_endOfTurnBonuses.Contains(normalized))
            {
                ShowHold(normalized, payload.PlayerId);
                return;
            }

            ShowTimed(normalized, _instantHideDelay, payload.PlayerId);
        }

        private void OnTurnChanged(string currentPlayerId)
        {
            if (!_holdUntilTurnEnd)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_holderPlayerId))
            {
                Hide();
                return;
            }

            if (!string.Equals(_holderPlayerId, currentPlayerId, StringComparison.Ordinal))
            {
                Hide();
            }
        }

        private void OnCardRequestState(CardRequestState state)
        {
            if (!state.HasEvent)
            {
                return;
            }

            var evt = state.LastEvent;
            if (evt.EventType != CardRequestEventType.Transferred &&
                evt.EventType != CardRequestEventType.Denied)
            {
                return;
            }

            if (_holdUntilTurnEnd &&
                !string.IsNullOrWhiteSpace(evt.AskerId) &&
                string.Equals(_holderPlayerId, evt.AskerId, StringComparison.Ordinal))
            {
                Hide();
            }
        }

        private void ShowTimed(string bonusType, float hideDelay, string playerId)
        {
            _holdUntilTurnEnd = false;
            _holderPlayerId = string.Empty;
            ShowInternal(bonusType, hideDelay, playerId);
        }

        private void ShowHold(string bonusType, string playerId)
        {
            _holdUntilTurnEnd = true;
            _holderPlayerId = playerId ?? string.Empty;
            ShowInternal(bonusType, null, playerId);
        }

        private void ShowInternal(string bonusType, float? hideDelay, string playerId)
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
            _holdUntilTurnEnd = false;
            _holderPlayerId = string.Empty;

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
