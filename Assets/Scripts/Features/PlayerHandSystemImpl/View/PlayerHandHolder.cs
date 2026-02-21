using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Features.CardRequestSystemImpl.View;
using Features.PlayerHandSystemImpl.Bootstrap;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Presenters;
using Features.PlayerHandSystemImpl.Storage;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.CardRequestSystem.Data;
using Modules.PlayerHand.Data;
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
        private BonusHandPresenter _bonusPresenter;
        private CardRequestPresentationPresenter _cardRequestPresenter;
        private BonusCardViewModelStore _bonusStore;
        private DiContainer _container;
        private RankStackView _standardViewPrefab;
        private BonusCardViewPool _bonusPool;
        private string _localPlayerId;
        private IDisposable _handChangedSubscription;
        private IDisposable _bonusChangedSubscription;
        private IDisposable _runtimeWatcher;
        private IDisposable _cardRequestedSubscription;
        private IDisposable _cardTransferredSubscription;
        private IDisposable _cardsReceivedSubscription;

        private readonly List<RowContainer> _rows = new();
        private readonly Dictionary<RankStackViewModel, RankStackView> _standardViews = new();
        private readonly Dictionary<BonusCardViewModel, BonusCardView> _bonusViews = new();
        private readonly Queue<PendingReceive> _pendingReceives = new();
        private bool _warnedMissingDeckOrigin;
        private readonly HashSet<RankStackViewModel> _outgoingStandard = new();
        private readonly Dictionary<RankStackViewModel, float> _outgoingStartedAt = new();
        private readonly HashSet<string> _transferOutRanks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, UniTaskCompletionSource> _pendingTransferOut = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _pendingSetCompleteRanks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _recentSetCompletedAt = new(StringComparer.OrdinalIgnoreCase);
        private RectTransform _cachedOpponentAnchor;
        private const float ExternalAnimationTimeoutSeconds = 2.5f;
        private const float OutgoingCleanupTimeoutSeconds = 6f;
        private const float OutgoingActiveAnimationGraceSeconds = 12f;
        private const float PendingReceiveMaxAgeSeconds = 2f;
        private const int PendingReceiveMaxAttempts = 20;
        private const float RecentSetCompleteWindowSeconds = 2f;

        private sealed class PendingReceive
        {
            public CardsReceivedEvent Event { get; }
            public float CreatedAt { get; }
            public int Attempts { get; set; }

            public PendingReceive(CardsReceivedEvent evt)
            {
                Event = evt;
                CreatedAt = Time.realtimeSinceStartup;
                Attempts = 0;
            }
        }

        private class RowContainer
        {
            public RectTransform Root;
            public HorizontalLayoutGroup Layout;
            public int Count;
        }

        [Inject]
        public void Construct(
            PlayerHandPresenter presenter,
            BonusHandPresenter bonusPresenter,
            CardRequestPresentationPresenter cardRequestPresenter,
            BonusCardViewModelStore bonusStore,
            DiContainer container,
            [Inject(Id = PlayerHandSystemMonoInstaller.StandardCardViewPrefabBindingId)] RankStackView standardViewPrefab,
            BonusCardViewPool bonusPool)
        {
            _presenter = presenter;
            _bonusPresenter = bonusPresenter;
            _cardRequestPresenter = cardRequestPresenter;
            _bonusStore = bonusStore;
            _container = container;
            _standardViewPrefab = standardViewPrefab;
            _bonusPool = bonusPool;
        }

        private void OnEnable()
        {
            if (_rowsRoot == null)
            {
                _rowsRoot = GetComponent<RectTransform>();
            }

            _runtimeWatcher = _presenter.IsActive.Subscribe(active =>
            {
                if (active)
                {
                    _localPlayerId = _presenter.LocalPlayerId;
                    RenderLayout();
                }
                else
                {
                    ReleaseAllViews();
                }
            });

            _handChangedSubscription = _presenter.HandChanged.Subscribe(_ => RenderLayout());
            _bonusChangedSubscription = _bonusStore.Changed.Subscribe(_ => RenderLayout());
            _cardRequestedSubscription = _cardRequestPresenter.CardRequested.Subscribe(OnCardRequested);
            _cardTransferredSubscription = _cardRequestPresenter.CardTransferred.Subscribe(OnCardTransferred);
            _cardsReceivedSubscription = _cardRequestPresenter.CardsReceived.Subscribe(OnCardsReceived);
            _localPlayerId = _presenter.LocalPlayerId;
        }

        private void OnDisable()
        {
            _runtimeWatcher?.Dispose();
            _runtimeWatcher = null;
            _handChangedSubscription?.Dispose();
            _handChangedSubscription = null;
            _bonusChangedSubscription?.Dispose();
            _bonusChangedSubscription = null;
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
            _recentSetCompletedAt.Clear();
            ReleaseAllViews();
        }

        private void Update()
        {
            if (_outgoingStandard.Count > 0)
            {
                var hadOutgoing = _outgoingStandard.Count > 0;
                PruneStaleOutgoingStandard();
                if (hadOutgoing && _outgoingStandard.Count == 0)
                {
                    RenderLayout();
                }
            }

            if (_pendingReceives.Count > 0)
            {
                TryApplyPendingReceives();
            }
        }

        private void RenderLayout()
        {
            if (!_presenter.IsActive.CurrentValue)
            {
                ReleaseAllViews();
                return;
            }

            var activeStandard = new HashSet<RankStackViewModel>(_presenter.StandardCards);
            var activeBonus = new HashSet<BonusCardViewModel>(_bonusPresenter.BonusCards);

            BeginOutgoingStandardRemovals(activeStandard);
            PruneStaleOutgoingStandard();

            if (_outgoingStandard.Count > 0)
            {
                EnsureActiveViews();
                TryApplyPendingReceives();
                return;
            }

            ResetRows();

            var slotIndex = 0;
            foreach (var viewModel in _presenter.StandardCards)
            {
                var parent = GetRowTransform(slotIndex++);
                if (!_standardViews.TryGetValue(viewModel, out var view))
                {
                    view = CreateStandardView(parent, viewModel);
                    if (view == null)
                    {
                        continue;
                    }
                    _standardViews[viewModel] = view;
                }
                else
                {
                    view.transform.SetParent(parent, false);
                    view.gameObject.SetActive(true);
                }
            }

            foreach (var viewModel in _bonusPresenter.BonusCards)
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
                SafeDespawnStandardView(view, string.Empty);
            }
            _standardViews.Clear();
            _outgoingStandard.Clear();
            _outgoingStartedAt.Clear();

            foreach (var view in _bonusViews.Values)
            {
                SafeDespawnBonusView(view);
            }
            _bonusViews.Clear();
        }

        private void RemoveMissingStandardViews(IReadOnlyCollection<RankStackViewModel> active)
        {
            foreach (var entry in _standardViews)
            {
                if (!active.Contains(entry.Key) && !_outgoingStandard.Contains(entry.Key))
                {
                    BeginStandardRemoval(entry.Key, entry.Value);
                }
            }
        }

        private void BeginOutgoingStandardRemovals(IReadOnlyCollection<RankStackViewModel> active)
        {
            var toRemove = new List<RankStackViewModel>();
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

        private void BeginStandardRemoval(RankStackViewModel viewModel, RankStackView view)
        {
            if (viewModel == null || view == null)
            {
                return;
            }

            _outgoingStandard.Add(viewModel);
            _outgoingStartedAt[viewModel] = Time.realtimeSinceStartup;
            view.gameObject.SetActive(true);
            AnimateAndDespawnStandard(viewModel, view).Forget();
        }

        private async UniTaskVoid AnimateAndDespawnStandard(RankStackViewModel viewModel, RankStackView view)
        {
            if (view == null)
            {
                return;
            }

            var rankKey = NormalizeRank(viewModel?.Rank);
            try
            {
                await WaitForTransferOutAsync(rankKey);
                await WaitForExternalAnimationsOrTimeoutAsync(view, rankKey);
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
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed to animate outgoing rank '{rankKey}': {ex.Message}");
            }
            finally
            {
                if (viewModel != null)
                {
                    _standardViews.Remove(viewModel);
                }

                RemoveOutgoingTracking(viewModel);
                ClearTransferredRank(rankKey);
                ClearSetCompleteRank(rankKey);
                SafeDespawnStandardView(view, rankKey);

                RenderLayout();
            }
        }

        private void PruneStaleOutgoingStandard()
        {
            if (_outgoingStandard.Count == 0)
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            var stale = new List<RankStackViewModel>();
            foreach (var viewModel in _outgoingStandard)
            {
                if (viewModel == null)
                {
                    stale.Add(viewModel);
                    continue;
                }

                if (!_standardViews.TryGetValue(viewModel, out var view) || view == null)
                {
                    stale.Add(viewModel);
                    continue;
                }

                if (!_outgoingStartedAt.TryGetValue(viewModel, out var startedAt))
                {
                    _outgoingStartedAt[viewModel] = now;
                    continue;
                }

                var age = now - startedAt;
                if (age < OutgoingCleanupTimeoutSeconds)
                {
                    continue;
                }

                if (view.HasActiveAnimations && age < OutgoingActiveAnimationGraceSeconds)
                {
                    continue;
                }

                var rankKey = NormalizeRank(viewModel.Rank);
                Debug.LogWarning(
                    $"[PlayerHand] Outgoing rank timeout for '{rankKey}' after {age:F2}s. Forcing cleanup.");
                _standardViews.Remove(viewModel);
                SafeDespawnStandardView(view, rankKey);
                ClearTransferredRank(rankKey);
                ClearSetCompleteRank(rankKey);
                stale.Add(viewModel);
            }

            foreach (var viewModel in stale)
            {
                RemoveOutgoingTracking(viewModel);
            }
        }

        private void RemoveOutgoingTracking(RankStackViewModel viewModel)
        {
            if (viewModel != null)
            {
                _outgoingStartedAt.Remove(viewModel);
            }

            _outgoingStandard.Remove(viewModel);
        }

        private void SafeDespawnStandardView(RankStackView view, string rankKey)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                Destroy(view.gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed to despawn rank '{rankKey}': {ex.Message}");
            }
        }

        private void SafeDespawnBonusView(BonusCardView view)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                _bonusPool.Despawn(view);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed to despawn bonus view: {ex.Message}");
            }
        }

        private async UniTask WaitForExternalAnimationsOrTimeoutAsync(RankStackView view, string rankKey)
        {
            if (view == null)
            {
                return;
            }

            var waitTask = view.WaitForExternalAnimationsAsync();
            var timeoutTask = UniTask.Delay(
                TimeSpan.FromSeconds(ExternalAnimationTimeoutSeconds),
                DelayType.UnscaledDeltaTime);
            var winner = await UniTask.WhenAny(waitTask, timeoutTask);
            if (winner == 0)
            {
                return;
            }

            Debug.LogWarning($"[PlayerHand] External animation timeout for rank='{rankKey}'. Forcing cleanup.");
            view.ForceCompleteExternalAnimations();
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
                    SafeDespawnBonusView(view);
                }
            }
        }

        private void OnCardsReceived(CardsReceivedEvent evt)
        {
            _pendingReceives.Enqueue(new PendingReceive(evt));
            TryApplyPendingReceives();
        }

        private RankStackView CreateStandardView(Transform parent, RankStackViewModel viewModel)
        {
            if (_container == null || _standardViewPrefab == null || viewModel == null)
            {
                Debug.LogWarning("[PlayerHand] Failed to create standard stack view: missing DI container or prefab.");
                return null;
            }

            var view = _container.InstantiatePrefabForComponent<RankStackView>(_standardViewPrefab, parent);
            if (view == null)
            {
                return null;
            }

            view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);
            view.Initialize(viewModel).Forget();
            return view;
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
                _localPlayerId = _presenter.LocalPlayerId;
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

        private void TryApplyPendingReceives()
        {
            if (_pendingReceives.Count == 0 || !_presenter.IsActive.CurrentValue)
            {
                return;
            }

            var safety = _pendingReceives.Count;
            while (_pendingReceives.Count > 0 && safety-- > 0)
            {
                var pending = _pendingReceives.Peek();
                if (!TryAnimateReceive(pending.Event))
                {
                    pending.Attempts++;
                    var age = Time.realtimeSinceStartup - pending.CreatedAt;
                    if (age >= PendingReceiveMaxAgeSeconds || pending.Attempts >= PendingReceiveMaxAttempts)
                    {
                        if (pending.Event.CompletedSet && !string.IsNullOrWhiteSpace(pending.Event.CompletedSetRank))
                        {
                            RegisterSetCompletionRank(pending.Event.CompletedSetRank);
                        }

                        Debug.LogWarning(
                            $"[PlayerHand] Dropping stale CardsReceived event after {age:F2}s and {pending.Attempts} attempts. " +
                            $"source={pending.Event.Source}, standard={pending.Event.StandardCards.Count}, bonus={pending.Event.BonusCards.Count}");
                        _pendingReceives.Dequeue();
                        continue;
                    }

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

            var standardToAnimate = BuildStandardReceiveList(evt);

            foreach (var card in standardToAnimate)
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

            foreach (var card in standardToAnimate)
            {
                PrepareStandardReceive(card);
            }

            foreach (var card in standardToAnimate)
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

            if (evt.CompletedSet && !string.IsNullOrWhiteSpace(evt.CompletedSetRank))
            {
                RegisterSetCompletionRank(evt.CompletedSetRank);
            }

            return true;
        }

        private List<StandardCardData> BuildStandardReceiveList(CardsReceivedEvent evt)
        {
            var result = new List<StandardCardData>();
            var completedRankKey = evt.CompletedSet ? NormalizeRank(evt.CompletedSetRank) : string.Empty;
            var completedRankAnimated = false;

            foreach (var card in evt.StandardCards)
            {
                if (ShouldSkipStandardReceive(evt, card.Rank))
                {
                    continue;
                }

                var rankKey = NormalizeRank(card.Rank);
                if (!string.IsNullOrWhiteSpace(completedRankKey) &&
                    string.Equals(rankKey, completedRankKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (completedRankAnimated)
                    {
                        continue;
                    }

                    completedRankAnimated = true;
                }

                result.Add(card);
            }

            return result;
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
            if (!TryGetStandardView(card.Rank, out var view))
            {
                return false;
            }

            var suitKey = NormalizeSuit(card.Suit);
            view.PlayReceiveFromAsync(origin, suitKey).Forget();
            return true;
        }

        private void PrepareStandardReceive(StandardCardData card)
        {
            if (!TryGetStandardView(card.Rank, out var view))
            {
                return;
            }

            var suitKey = NormalizeSuit(card.Suit);
            view.PrepareReceive(suitKey);
        }

        private bool TryAnimateBonusReceive(BonusCardData card, RectTransform origin)
        {
            var typeKey = NormalizeBonus(card.BonusType);
            foreach (var viewModel in _bonusPresenter.BonusCards)
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
            return TryGetStandardView(card.Rank, out _);
        }

        private bool CanAnimateBonusReceive(BonusCardData card)
        {
            var typeKey = NormalizeBonus(card.BonusType);
            foreach (var viewModel in _bonusPresenter.BonusCards)
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
            await UniTask.WhenAny(transferTask, UniTask.Delay(TimeSpan.FromSeconds(2), DelayType.UnscaledDeltaTime));
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

        private bool WasSetCompletedRecently(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            var rankKey = NormalizeRank(rank);
            if (!_recentSetCompletedAt.TryGetValue(rankKey, out var completedAt))
            {
                return false;
            }

            var now = Time.realtimeSinceStartup;
            if (now - completedAt <= RecentSetCompleteWindowSeconds)
            {
                return true;
            }

            _recentSetCompletedAt.Remove(rankKey);
            return false;
        }

        private bool ShouldSkipStandardReceive(CardsReceivedEvent evt, string rank)
        {
            if (evt.CompletedSet &&
                !string.IsNullOrWhiteSpace(evt.CompletedSetRank) &&
                string.Equals(
                    NormalizeRank(rank),
                    NormalizeRank(evt.CompletedSetRank),
                    StringComparison.OrdinalIgnoreCase))
            {
                // For the set-closing card event we still want to show receive animation
                // before set-complete removal starts.
                return false;
            }

            return IsSetCompleteRank(rank) || WasSetCompletedRecently(rank);
        }

        private void RegisterSetCompletionRank(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            _pendingSetCompleteRanks.Add(rankKey);
            _recentSetCompletedAt[rankKey] = Time.realtimeSinceStartup;
        }

        private bool TryGetStandardView(string rank, out RankStackView view)
        {
            view = null;
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return false;
            }

            foreach (var entry in _standardViews)
            {
                var viewModel = entry.Key;
                var candidateView = entry.Value;
                if (viewModel == null || candidateView == null)
                {
                    continue;
                }

                if (!string.Equals(viewModel.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                view = candidateView;
                return true;
            }

            return false;
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

                await UniTask.Delay(TimeSpan.FromMilliseconds(50), DelayType.UnscaledDeltaTime);
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
