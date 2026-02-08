using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.CardRequestSystemImpl.View;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Presenters;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.CardRequestSystem.Data;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using Modules.PlayerHand.Data;
using Modules.PlayerHand.Services;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.PlayerHandSystemImpl.View
{
    public class PlayerHandHolder : MonoBehaviour
    {
        [SerializeField] private RectTransform _rowsRoot;
        [SerializeField] private RectTransform _opponentHandAnchor;
        [SerializeField] private CardTransferAnimator _cardTransferAnimator;
        [SerializeField, Min(1)] private int _maxItemsPerRow = 5;
        [SerializeField] private float _rowItemSpacing = 12f;
        [SerializeField] private TextAnchor _rowAlignment = TextAnchor.MiddleCenter;
        [SerializeField] private RectTransform _deckOrigin;

        private PlayerHandPresenter _presenter;
        private RankStackViewPool _standardPool;
        private BonusCardViewPool _bonusPool;
        private PlayerHandService _handService;
        private LobbyService _lobbyService;
        private string _localPlayerId;
        private PlayerHandPresenterState _state;
        private IDisposable _handChangedSubscription;
        private IDisposable _runtimeWatcher;
        private IDisposable _cardsReceivedSubscription;
        private IDisposable _cardRequestedSubscription;
        private IDisposable _cardTransferredSubscription;

        private readonly List<RowContainer> _rows = new();
        private readonly Dictionary<StandardCardViewModel, RankStackView> _standardViews = new();
        private readonly Dictionary<BonusCardViewModel, BonusCardView> _bonusViews = new();
        private readonly Queue<CardsReceivedEvent> _pendingReceives = new();
        private bool _warnedMissingDeckOrigin;
        private readonly HashSet<StandardCardViewModel> _outgoingStandard = new();
        private readonly HashSet<string> _transferOutRanks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, UniTaskCompletionSource> _pendingTransferOut = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _pendingSetCompleteRanks = new(StringComparer.OrdinalIgnoreCase);
        private IDisposable _setCompleteSubscription;
        private RectTransform _cachedOpponentAnchor;

        private class RowContainer
        {
            public RectTransform Root;
            public HorizontalLayoutGroup Layout;
            public int Count;
        }

        [Inject]
        public void Construct(
            PlayerHandPresenter presenter,
            RankStackViewPool standardPool,
            BonusCardViewPool bonusPool,
            PlayerHandService handService,
            LobbyService lobbyService)
        {
            _presenter = presenter;
            _standardPool = standardPool;
            _bonusPool = bonusPool;
            _handService = handService;
            _lobbyService = lobbyService;
        }

        private void OnEnable()
        {
            if (_rowsRoot == null)
            {
                _rowsRoot = GetComponent<RectTransform>();
            }

            _runtimeWatcher = _presenter.State.Subscribe(OnStateChanged);
            OnStateChanged(_presenter.CurrentState);

            _localPlayerId = _lobbyService?.GetLocalPlayer().Id;
            if (_lobbyService != null)
            {
                _setCompleteSubscription = _lobbyService.ChestUpdated
                    .Subscribe(OnSetCompleted);
            }
            if (_handService != null)
            {
                _cardsReceivedSubscription = _handService.CardsReceived
                    .Subscribe(OnCardsReceived);
            }
        }

        private void OnDisable()
        {
            _runtimeWatcher?.Dispose();
            _runtimeWatcher = null;
            _setCompleteSubscription?.Dispose();
            _setCompleteSubscription = null;
            _cardsReceivedSubscription?.Dispose();
            _cardsReceivedSubscription = null;
            _cardRequestedSubscription?.Dispose();
            _cardRequestedSubscription = null;
            _cardTransferredSubscription?.Dispose();
            _cardTransferredSubscription = null;
            _pendingReceives.Clear();
            _transferOutRanks.Clear();
            _pendingTransferOut.Clear();
            _pendingSetCompleteRanks.Clear();

            OnStateChanged(null);
        }

        private void OnStateChanged(PlayerHandPresenterState state)
        {
            if (ReferenceEquals(_state, state))
            {
                return;
            }

            _handChangedSubscription?.Dispose();
            _handChangedSubscription = null;
            _cardRequestedSubscription?.Dispose();
            _cardRequestedSubscription = null;
            _cardTransferredSubscription?.Dispose();
            _cardTransferredSubscription = null;

            _state = state;
            RenderLayout();

            if (_state != null)
            {
                _handChangedSubscription = _state.HandChanged.Subscribe(_ => RenderLayout());
                _cardRequestedSubscription = _state.CardRequested.Subscribe(OnCardRequested);
                _cardTransferredSubscription = _state.CardTransferred.Subscribe(OnCardTransferred);
            }
        }

        private void RenderLayout()
        {
            if (_state == null)
            {
                ReleaseAllViews();
                return;
            }

            var activeStandard = new HashSet<StandardCardViewModel>(_state.StandardCards);
            var activeBonus = new HashSet<BonusCardViewModel>(_state.BonusCards);

            BeginOutgoingStandardRemovals(activeStandard);

            if (_outgoingStandard.Count > 0)
            {
                EnsureActiveViews();
                TryApplyPendingReceives();
                return;
            }

            ResetRows();

            var slotIndex = 0;
            foreach (var viewModel in _state.StandardCards)
            {
                var parent = GetRowTransform(slotIndex++);
                if (!_standardViews.TryGetValue(viewModel, out var view))
                {
                    view = _standardPool.Spawn(parent, viewModel);
                    _standardViews[viewModel] = view;
                }
                else
                {
                    view.transform.SetParent(parent, false);
                    view.gameObject.SetActive(true);
                }
            }

            foreach (var viewModel in _state.BonusCards)
            {
                var parent = GetRowTransform(slotIndex++);
                if (!_bonusViews.TryGetValue(viewModel, out var view))
                {
                    view = _bonusPool.Spawn(parent, viewModel);
                    _bonusViews[viewModel] = view;
                }
                else
                {
                    view.transform.SetParent(parent, false);
                    view.gameObject.SetActive(true);
                }
            }

            RemoveMissingStandardViews(activeStandard);
            RemoveMissingBonusViews(activeBonus);
            ForceRebuildLayout();
            TryApplyPendingReceives();
        }

        private void ResetRows()
        {
            foreach (var row in _rows)
            {
                row.Count = 0;
                if (!RowHasOutgoing(row.Root))
                {
                    row.Root.gameObject.SetActive(false);
                }
            }
        }

        private RectTransform GetRowTransform(int slotIndex)
        {
            var itemsPerRow = Mathf.Max(1, _maxItemsPerRow);
            var rowIndex = slotIndex / itemsPerRow;
            EnsureRow(rowIndex);
            var row = _rows[rowIndex];
            row.Count++;
            row.Root.gameObject.SetActive(true);
            return row.Root;
        }

        private void EnsureRow(int rowIndex)
        {
            while (_rows.Count <= rowIndex)
            {
                _rows.Add(CreateRow(_rows.Count));
            }
        }

        private RowContainer CreateRow(int index)
        {
            var go = new GameObject($"Row_{index}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_rowsRoot, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = _rowItemSpacing;
            layout.childAlignment = _rowAlignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return new RowContainer
            {
                Root = rect,
                Layout = layout,
                Count = 0
            };
        }

        private void ReleaseAllViews()
        {
            foreach (var view in _standardViews.Values)
            {
                _standardPool.Despawn(view);
            }
            _standardViews.Clear();

            foreach (var view in _bonusViews.Values)
            {
                _bonusPool.Despawn(view);
            }
            _bonusViews.Clear();
        }

        private void RemoveMissingStandardViews(IReadOnlyCollection<StandardCardViewModel> active)
        {
            foreach (var entry in _standardViews)
            {
                if (!active.Contains(entry.Key) && !_outgoingStandard.Contains(entry.Key))
                {
                    BeginStandardRemoval(entry.Key, entry.Value);
                }
            }
        }

        private void BeginOutgoingStandardRemovals(IReadOnlyCollection<StandardCardViewModel> active)
        {
            var toRemove = new List<StandardCardViewModel>();
            foreach (var entry in _standardViews)
            {
                if (!active.Contains(entry.Key) && !_outgoingStandard.Contains(entry.Key))
                {
                    toRemove.Add(entry.Key);
                }
            }

            foreach (var viewModel in toRemove)
            {
                if (_standardViews.TryGetValue(viewModel, out var view))
                {
                    BeginStandardRemoval(viewModel, view);
                }
            }
        }

        private void BeginStandardRemoval(StandardCardViewModel viewModel, RankStackView view)
        {
            if (viewModel == null || view == null)
            {
                return;
            }

            _outgoingStandard.Add(viewModel);
            view.gameObject.SetActive(true);
            AnimateAndDespawnStandard(viewModel, view).Forget();
        }

        private async UniTaskVoid AnimateAndDespawnStandard(StandardCardViewModel viewModel, RankStackView view)
        {
            if (view == null)
            {
                return;
            }

            var rankKey = NormalizeRank(viewModel?.Rank);
            await WaitForTransferOutAsync(rankKey);
            await view.WaitForExternalAnimationsAsync();
            if (!view.HasPendingSetComplete && !IsSetCompleteRank(rankKey))
            {
                await WaitForSetCompleteAsync(rankKey);
            }
            if (IsRankTransferredOut(rankKey))
            {
                view.MarkTransferredOut();
            }
            if (view.HasPendingSetComplete || IsSetCompleteRank(rankKey))
            {
                await view.PlaySetCompleteAnimationAsync();
            }
            else if (!view.SkipRemovalAnimation)
            {
                await view.PlayTransferRemovalAnimationAsync();
            }
            else
            {
                // Skip fade-out when transfer animation already played.
            }

            _standardPool.Despawn(view);
            if (viewModel != null)
            {
                _standardViews.Remove(viewModel);
                _outgoingStandard.Remove(viewModel);
            }
            ClearTransferredRank(rankKey);
            ClearSetCompleteRank(rankKey);

            RenderLayout();
        }

        private void RemoveMissingBonusViews(IReadOnlyCollection<BonusCardViewModel> active)
        {
            var toRemove = new List<BonusCardViewModel>();
            foreach (var entry in _bonusViews)
            {
                if (!active.Contains(entry.Key))
                {
                    toRemove.Add(entry.Key);
                }
            }

            foreach (var viewModel in toRemove)
            {
                if (_bonusViews.TryGetValue(viewModel, out var view))
                {
                    _bonusViews.Remove(viewModel);
                    _bonusPool.Despawn(view);
                }
            }
        }

        private void OnCardsReceived(CardsReceivedEvent evt)
        {
            if (string.IsNullOrWhiteSpace(_localPlayerId))
            {
                _localPlayerId = _lobbyService?.GetLocalPlayer().Id;
            }

            if (string.IsNullOrWhiteSpace(_localPlayerId) ||
                !string.Equals(evt.PlayerId, _localPlayerId, StringComparison.Ordinal))
            {
                return;
            }

            _pendingReceives.Enqueue(evt);
            TryApplyPendingReceives();
        }

        private void OnCardRequested(CardRequestEvent evt)
        {
            // Placeholder for future UI hooks.
        }

        private void OnCardTransferred(CardRequestEvent evt)
        {
            if (_cardTransferAnimator == null)
            {
                _cardTransferAnimator = FindObjectOfType<CardTransferAnimator>();
                if (_cardTransferAnimator == null)
                {
                    return;
                }
            }

            if (string.Equals(evt.TargetId, "chest", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_localPlayerId))
            {
                _localPlayerId = _lobbyService?.GetLocalPlayer().Id;
            }

            _cardTransferAnimator.AnimateTransfer(evt, _localPlayerId);
        }

        public void MarkRankTransferredOut(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            _transferOutRanks.Add(rankKey);
            if (!_pendingTransferOut.ContainsKey(rankKey))
            {
                _pendingTransferOut[rankKey] = new UniTaskCompletionSource();
            }
        }

        public void CompleteRankTransfer(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            if (_pendingTransferOut.TryGetValue(rankKey, out var tcs))
            {
                tcs.TrySetResult();
                _pendingTransferOut.Remove(rankKey);
            }
        }

        private void OnSetCompleted(LobbyChestUpdatedPayload payload)
        {
            if (payload.PlayerId == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_localPlayerId))
            {
                _localPlayerId = _lobbyService?.GetLocalPlayer().Id;
            }

            if (string.IsNullOrWhiteSpace(_localPlayerId) ||
                !string.Equals(payload.PlayerId, _localPlayerId, StringComparison.Ordinal))
            {
                return;
            }

            var rankKey = NormalizeRank(payload.Rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            _pendingSetCompleteRanks.Add(rankKey);
        }

        private void TryApplyPendingReceives()
        {
            if (_pendingReceives.Count == 0 || _state == null)
            {
                return;
            }

            var safety = _pendingReceives.Count;
            while (_pendingReceives.Count > 0 && safety-- > 0)
            {
                var evt = _pendingReceives.Peek();
                if (!TryAnimateReceive(evt))
                {
                    return;
                }

                _pendingReceives.Dequeue();
            }
        }

        private bool TryAnimateReceive(CardsReceivedEvent evt)
        {
            var origin = ResolveReceiveOrigin(evt.Source);
            if (origin == null)
            {
                return false;
            }

            foreach (var card in evt.StandardCards)
            {
                if (!CanAnimateStandardReceive(card))
                {
                    return false;
                }
            }

            foreach (var card in evt.BonusCards)
            {
                if (!CanAnimateBonusReceive(card))
                {
                    return false;
                }
            }

            foreach (var card in evt.StandardCards)
            {
                PrepareStandardReceive(card);
            }

            foreach (var card in evt.StandardCards)
            {
                if (!TryAnimateStandardReceive(card, origin))
                {
                    return false;
                }
            }

            foreach (var card in evt.BonusCards)
            {
                if (!TryAnimateBonusReceive(card, origin))
                {
                    return false;
                }
            }

            return true;
        }

        private RectTransform ResolveReceiveOrigin(string source)
        {
            if (string.Equals(source, "deck", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveDeckOrigin();
            }

            return ResolveOpponentHandAnchor();
        }

        private bool TryAnimateStandardReceive(StandardCardData card, RectTransform origin)
        {
            var rankKey = NormalizeRank(card.Rank);
            foreach (var viewModel in _state.StandardCards)
            {
                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out var view))
                {
                    var suitKey = NormalizeSuit(card.Suit);
                    view.PlayReceiveFromAsync(origin, suitKey).Forget();
                    return true;
                }
            }

            return false;
        }

        private void PrepareStandardReceive(StandardCardData card)
        {
            var rankKey = NormalizeRank(card.Rank);
            foreach (var viewModel in _state.StandardCards)
            {
                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out var view) && view != null)
                {
                    var suitKey = NormalizeSuit(card.Suit);
                    view.PrepareReceive(suitKey);
                    return;
                }
            }
        }

        private bool TryAnimateBonusReceive(BonusCardData card, RectTransform origin)
        {
            var typeKey = NormalizeBonus(card.BonusType);
            foreach (var viewModel in _state.BonusCards)
            {
                if (!string.Equals(viewModel.BonusCardType, typeKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_bonusViews.TryGetValue(viewModel, out var view))
                {
                    view.PlayReceiveFromAsync(origin).Forget();
                    return true;
                }
            }

            return false;
        }

        private bool CanAnimateStandardReceive(StandardCardData card)
        {
            var rankKey = NormalizeRank(card.Rank);
            foreach (var viewModel in _state.StandardCards)
            {
                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out var view) && view != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanAnimateBonusReceive(BonusCardData card)
        {
            var typeKey = NormalizeBonus(card.BonusType);
            foreach (var viewModel in _state.BonusCards)
            {
                if (!string.Equals(viewModel.BonusCardType, typeKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_bonusViews.TryGetValue(viewModel, out var view) && view != null)
                {
                    return true;
                }
            }

            return false;
        }

        private RectTransform ResolveDeckOrigin()
        {
            if (_deckOrigin != null)
            {
                return _deckOrigin;
            }

            var fallback = _rowsRoot != null ? _rowsRoot : transform as RectTransform;
            if (fallback == null)
            {
                return null;
            }

            if (!_warnedMissingDeckOrigin)
            {
                _warnedMissingDeckOrigin = true;
                Debug.LogWarning("[PlayerHand] Deck origin is not assigned, using hand root as fallback.");
            }

            return fallback;
        }

        private bool RowHasOutgoing(RectTransform rowRoot)
        {
            if (rowRoot == null || _outgoingStandard.Count == 0)
            {
                return false;
            }

            foreach (var viewModel in _outgoingStandard)
            {
                if (viewModel == null)
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out var view) &&
                    view != null &&
                    view.transform.parent == rowRoot)
                {
                    return true;
                }
            }

            return false;
        }

        private void ForceRebuildLayout()
        {
            if (_rowsRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);
        }

        private RectTransform ResolveOpponentHandAnchor()
        {
            if (_opponentHandAnchor != null)
            {
                return _opponentHandAnchor;
            }

            if (_cachedOpponentAnchor != null)
            {
                return _cachedOpponentAnchor;
            }

            var animator = _cardTransferAnimator;
            if (animator == null)
            {
                animator = FindObjectOfType<CardTransferAnimator>();
            }

            _cachedOpponentAnchor = animator != null ? animator.OpponentHandAnchor : null;
            return _cachedOpponentAnchor;
        }

        private void EnsureActiveViews()
        {
            foreach (var entry in _standardViews)
            {
                if (entry.Value != null)
                {
                    entry.Value.gameObject.SetActive(true);
                }
            }

            foreach (var entry in _bonusViews)
            {
                if (entry.Value != null)
                {
                    entry.Value.gameObject.SetActive(true);
                }
            }

            foreach (var row in _rows)
            {
                if (row?.Root == null)
                {
                    continue;
                }

                if (row.Root.childCount > 0)
                {
                    row.Root.gameObject.SetActive(true);
                }
            }
        }

        private bool IsRankTransferredOut(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            return _transferOutRanks.Contains(NormalizeRank(rank));
        }

        private void ClearTransferredRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            _transferOutRanks.Remove(NormalizeRank(rank));
        }

        private async UniTask WaitForTransferOutAsync(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            UniTaskCompletionSource tcs = null;
            var timeout = Time.realtimeSinceStartup + 0.5f;
            while (!_pendingTransferOut.TryGetValue(rankKey, out tcs) &&
                   Time.realtimeSinceStartup < timeout)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (tcs == null)
            {
                return;
            }

            var transferTask = tcs.Task.Preserve();
            await UniTask.WhenAny(transferTask, UniTask.Delay(TimeSpan.FromSeconds(2)));
            _pendingTransferOut.Remove(rankKey);
        }

        private bool IsSetCompleteRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            return _pendingSetCompleteRanks.Contains(NormalizeRank(rank));
        }

        private async UniTask WaitForSetCompleteAsync(string rankKey)
        {
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            var elapsed = 0f;
            while (elapsed < 0.5f)
            {
                if (IsSetCompleteRank(rankKey))
                {
                    return;
                }

                await UniTask.Delay(TimeSpan.FromMilliseconds(50));
                elapsed += 0.05f;
            }
        }

        private void ClearSetCompleteRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            _pendingSetCompleteRanks.Remove(NormalizeRank(rank));
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank) ? string.Empty : rank.Trim().ToLowerInvariant();
        }

        private static string NormalizeSuit(string suit)
        {
            return string.IsNullOrWhiteSpace(suit) ? string.Empty : suit.Trim().ToLowerInvariant();
        }

        private static string NormalizeBonus(string bonusType)
        {
            return string.IsNullOrWhiteSpace(bonusType) ? string.Empty : bonusType.Trim();
        }

        public bool TryGetStandardCardImage(string rank, string suit, out Image image)
        {
            image = null;
            var rankKey = NormalizeRank(rank);
            foreach (var viewModel in _standardViews.Keys)
            {
                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out var view) &&
                    view != null &&
                    view.TryGetCardImage(suit, out image))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetStandardCardImage(string rank, out Image image)
        {
            return TryGetStandardCardImage(rank, string.Empty, out image);
        }

        public bool TryGetStandardCardView(string rank, out RankStackView view)
        {
            view = null;
            var rankKey = NormalizeRank(rank);
            foreach (var viewModel in _standardViews.Keys)
            {
                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_standardViews.TryGetValue(viewModel, out view) && view != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetBonusView(string bonusType, out BonusCardView view)
        {
            view = null;
            var typeKey = NormalizeBonus(bonusType);
            foreach (var viewModel in _bonusViews.Keys)
            {
                if (!string.Equals(viewModel.BonusCardType, typeKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_bonusViews.TryGetValue(viewModel, out view) && view != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
