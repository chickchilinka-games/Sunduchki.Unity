using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Features.CardRequestSystemImpl.View;
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
    public partial class RankStackView : MonoBehaviour
    {
        private static readonly Color DisabledTint = new(0.65f, 0.65f, 0.65f, 1f);

        [SerializeField] private Image[] _cards;
        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _canvasGroup;
        [Header("Receive Animation")]
        [SerializeField] private float _receiveOffsetY = 24f;
        [SerializeField] private float _receiveOffsetXFromOpponent = 36f;
        [SerializeField] private float _receiveOffsetYFromDeck = -32f;
        [SerializeField, Min(0.01f)] private float _receiveDuration = 0.28f;
        [SerializeField, Min(0.01f)] private float _setCompleteFlashDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float _setCompleteFadeDuration = 0.2f;
        [SerializeField, Min(0.05f)] private float _resolveCardTimeout = 1.2f;
        [SerializeField, Min(0.01f)] private float _resolveCardRetryDelay = 0.03f;
        [SerializeField, Min(0.01f)] private float _newCardAutoRevealDelay = 0.12f;
        [SerializeField, Min(0.1f)] private float _recentlyRemovedSuitTtl = 1.5f;
        [SerializeField, Min(0.01f)] private float _tweenTimeoutBuffer = 0.35f;

        private readonly List<ManagedAsset<Sprite>> _cardSprites = new();
        private readonly Dictionary<Image, Vector2> _cardBasePositions = new();
        private readonly List<string> _resolvedSuits = new();
        private readonly HashSet<string> _hiddenReceiveSuits = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _activeReceiveSuits = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _pendingAutoRevealSuits = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string Suit, float Timestamp)> _recentlyRemovedSnapshotSuits = new();
        private readonly Queue<RankStackCommand> _queuedCommands = new();
        private readonly HashSet<string> _processedTransferActionIds = new(StringComparer.Ordinal);
        private readonly RankStackTweenAnimator _tweenAnimator = new();

        private CardSpriteResolver _cardSpriteResolver;
        private RankStackViewModel _viewModel;
        private CompositeDisposable _bindings;
        private CancellationTokenSource _bindingsCts;
        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private Func<RectTransform> _deckReceiveOriginProvider;
        private Func<RectTransform> _opponentReceiveOriginProvider;
        private CardTransferAnimator _cardTransferAnimator;
        private bool _isCommandLoopRunning;
        private bool _removalRequested;
        private bool _removalNotified;
        private bool _setCompletePlayed;
        private int _bindingVersion;
        private long _lastAppliedSnapshotRevision;

        public event Action<RankStackView> RemovalReady;

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

        public UniTask Initialize(RankStackViewModel viewModel)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            ResetBindings();
            ResetVisualState();

            _viewModel = viewModel;
            _bindingsCts = new CancellationTokenSource();
            _bindings = new CompositeDisposable();

            viewModel.CanPress
                .Subscribe(SetInteractable)
                .AddTo(_bindings);

            viewModel.CommandQueued
                .Subscribe(_ =>
                {
                    EnqueueViewModelCommands();
                    RunCommandLoopIfNeeded().Forget();
                })
                .AddTo(_bindings);

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => _viewModel?.Press());
            }

            EnqueueViewModelCommands();
            RunCommandLoopIfNeeded().Forget();
            return UniTask.CompletedTask;
        }

        public void ResetView()
        {
            ResetBindings();
            ResetVisualState();
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

        public void ConfigureRuntime(
            CardTransferAnimator cardTransferAnimator,
            Func<RectTransform> deckReceiveOriginProvider,
            Func<RectTransform> opponentReceiveOriginProvider)
        {
            _cardTransferAnimator = cardTransferAnimator;
            _deckReceiveOriginProvider = deckReceiveOriginProvider;
            _opponentReceiveOriginProvider = opponentReceiveOriginProvider;
        }

        public void RequestRemoval()
        {
            _removalRequested = true;
            _removalNotified = false;
            NotifyRemovalReadyIfPossible();
        }

        public void EnqueueTransferOutCommand(IReadOnlyList<string> suits, int count, string actionId)
        {
            EnqueueCommand(RankStackCommand.TransferOut(suits, count, actionId));
            RunCommandLoopIfNeeded().Forget();
        }

        public bool TryGetCardImage(string suit, out Image image)
        {
            image = null;
            if (_cards == null || _cards.Length == 0)
            {
                return false;
            }

            var normalizedSuit = NormalizeSuit(suit);
            if (string.IsNullOrWhiteSpace(normalizedSuit))
            {
                return false;
            }

            if (!TryResolveCardIndexBySuit(normalizedSuit, out var index))
            {
                return false;
            }

            if (index < 0 || index >= _cards.Length)
            {
                return false;
            }

            var card = _cards[index];
            if (card == null || card.sprite == null || !card.gameObject.activeSelf)
            {
                return false;
            }

            image = card;
            return true;
        }

        public void SetTint(Color color)
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

        private void SetInteractable(bool canPress)
        {
            if (_button != null)
            {
                _button.interactable = canPress;
            }

            if (_canvasGroup != null && !_setCompletePlayed)
            {
                _canvasGroup.alpha = 1f;
            }

            SetTint(canPress ? Color.white : DisabledTint);
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

        private void ResetBindings()
        {
            _bindings?.Dispose();
            _bindings = null;
            _bindingsCts?.Cancel();
            _bindingsCts?.Dispose();
            _bindingsCts = null;
            _viewModel = null;
            _bindingVersion++;
            _isCommandLoopRunning = false;
        }

        private void ResetVisualState()
        {
            _removalRequested = false;
            _removalNotified = false;
            _setCompletePlayed = false;
            _lastAppliedSnapshotRevision = 0;
            _hiddenReceiveSuits.Clear();
            _activeReceiveSuits.Clear();
            _pendingAutoRevealSuits.Clear();
            _recentlyRemovedSnapshotSuits.Clear();
            _queuedCommands.Clear();
            _processedTransferActionIds.Clear();

            if (_canvasGroup != null)
            {
                DOTween.Kill(_canvasGroup);
                _canvasGroup.alpha = 1f;
            }

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
                DOTween.Kill(card.rectTransform);
                if (_cardBasePositions.TryGetValue(card, out var basePos))
                {
                    card.rectTransform.anchoredPosition = basePos;
                }

                SetImageAlpha(card, 1f);
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

        private void NotifyRemovalReadyIfPossible()
        {
            if (!_removalRequested || _removalNotified || _isCommandLoopRunning || _viewModel == null)
            {
                return;
            }

            if (_viewModel.HasPendingCommands)
            {
                return;
            }

            if (_queuedCommands.Count > 0)
            {
                return;
            }

            _removalNotified = true;
            RemovalReady?.Invoke(this);
        }

        private void TrackRemovedSnapshotSuits(IReadOnlyList<string> incomingSuits)
        {
            CleanupRecentlyRemovedSnapshotSuits();
            if (_resolvedSuits.Count == 0)
            {
                return;
            }

            var incoming = new HashSet<string>(NormalizeSuits(incomingSuits), StringComparer.OrdinalIgnoreCase);
            var now = Time.unscaledTime;
            foreach (var suit in _resolvedSuits)
            {
                if (string.IsNullOrWhiteSpace(suit) || incoming.Contains(suit))
                {
                    continue;
                }

                var exists = _recentlyRemovedSnapshotSuits.Any(entry =>
                    string.Equals(entry.Suit, suit, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    _recentlyRemovedSnapshotSuits.Add((suit, now));
                }
            }
        }

        private void CleanupRecentlyRemovedSnapshotSuits()
        {
            if (_recentlyRemovedSnapshotSuits.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            for (var i = _recentlyRemovedSnapshotSuits.Count - 1; i >= 0; i--)
            {
                var entry = _recentlyRemovedSnapshotSuits[i];
                if (now - entry.Timestamp > _recentlyRemovedSuitTtl)
                {
                    _recentlyRemovedSnapshotSuits.RemoveAt(i);
                }
            }
        }

        private bool TryConsumeRecentlyRemovedSuit(List<string> pendingSuits, out string suit)
        {
            suit = string.Empty;
            CleanupRecentlyRemovedSnapshotSuits();
            if (_recentlyRemovedSnapshotSuits.Count == 0)
            {
                return false;
            }

            if (pendingSuits != null && pendingSuits.Count > 0)
            {
                var expectedSuit = NormalizeSuit(pendingSuits[0]);
                for (var i = 0; i < _recentlyRemovedSnapshotSuits.Count; i++)
                {
                    var entry = _recentlyRemovedSnapshotSuits[i];
                    if (!string.Equals(entry.Suit, expectedSuit, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    suit = entry.Suit;
                    _recentlyRemovedSnapshotSuits.RemoveAt(i);
                    pendingSuits.RemoveAt(0);
                    return true;
                }
            }

            if (pendingSuits == null || pendingSuits.Count == 0)
            {
                suit = _recentlyRemovedSnapshotSuits[0].Suit;
                _recentlyRemovedSnapshotSuits.RemoveAt(0);
                return true;
            }

            return false;
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

        private static List<string> NormalizeSuits(IReadOnlyList<string> suits)
        {
            var normalized = new List<string>();
            if (suits == null)
            {
                return normalized;
            }

            for (var i = 0; i < suits.Count; i++)
            {
                var suit = NormalizeSuit(suits[i]);
                if (string.IsNullOrWhiteSpace(suit))
                {
                    continue;
                }

                normalized.Add(suit);
            }

            return normalized;
        }
    }
}
