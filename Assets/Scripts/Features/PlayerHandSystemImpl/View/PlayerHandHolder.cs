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
        private IDisposable _runtimeWatcher;
        private IDisposable _handChangedSubscription;
        private IDisposable _bonusChangedSubscription;
        private IDisposable _cardRequestedSubscription;
        private IDisposable _cardTransferredSubscription;
        private IDisposable _cardsReceivedSubscription;

        private readonly List<RowContainer> _rows = new();
        private readonly Dictionary<RankStackViewModel, RankStackView> _standardViews = new();
        private readonly Dictionary<BonusCardViewModel, BonusCardView> _bonusViews = new();
        private readonly HashSet<RankStackViewModel> _removingStandard = new();
        private readonly HashSet<RankStackViewModel> _waitingStandardAnimation = new();
        private readonly HashSet<string> _transferredRanks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _activeTransferOutByRank = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _transferOutReleaseAt = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _removalStartGraceUntil = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _removalStartGracePending = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _pendingSetCompletionRanks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<PendingBonusReceive> _pendingBonusReceives = new();
        private readonly List<PendingStandardReceive> _pendingStandardReceives = new();

        private RectTransform _cachedOpponentAnchor;
        private bool _warnedMissingDeckOrigin;
        private const float PendingBonusReceiveMaxAgeSeconds = 8f;
        private const float PendingStandardReceiveMaxAgeSeconds = 8f;
        private const float TransferOutReleaseDelaySeconds = 0.2f;
        private const float RemovalStartGraceSeconds = 0.2f;

        private sealed class RowContainer
        {
            public RectTransform Root;
            public HorizontalLayoutGroup Layout;
            public int Count;
        }

        private readonly struct PendingBonusReceive
        {
            public string BonusType { get; }
            public string Source { get; }
            public float CreatedAt { get; }

            public PendingBonusReceive(string bonusType, string source, float createdAt)
            {
                BonusType = bonusType ?? string.Empty;
                Source = source ?? string.Empty;
                CreatedAt = createdAt;
            }
        }

        private readonly struct PendingStandardReceive
        {
            public string Rank { get; }
            public string Suit { get; }
            public string Source { get; }
            public long EventSeq { get; }
            public bool CompletedSet { get; }
            public float CreatedAt { get; }

            public PendingStandardReceive(
                string rank,
                string suit,
                string source,
                long eventSeq,
                bool completedSet,
                float createdAt)
            {
                Rank = rank ?? string.Empty;
                Suit = suit ?? string.Empty;
                Source = source ?? string.Empty;
                EventSeq = eventSeq;
                CompletedSet = completedSet;
                CreatedAt = createdAt;
            }
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
            if (_presenter.IsActive.CurrentValue)
            {
                RenderLayout();
            }
        }

        private void OnDisable()
        {
            _runtimeWatcher?.Dispose();
            _runtimeWatcher = null;

            _handChangedSubscription?.Dispose();
            _handChangedSubscription = null;
            _bonusChangedSubscription?.Dispose();
            _bonusChangedSubscription = null;
            _cardRequestedSubscription?.Dispose();
            _cardRequestedSubscription = null;
            _cardTransferredSubscription?.Dispose();
            _cardTransferredSubscription = null;
            _cardsReceivedSubscription?.Dispose();
            _cardsReceivedSubscription = null;

            _transferredRanks.Clear();
            _activeTransferOutByRank.Clear();
            _transferOutReleaseAt.Clear();
            _removalStartGraceUntil.Clear();
            _removalStartGracePending.Clear();
            _removingStandard.Clear();
            _waitingStandardAnimation.Clear();
            _pendingSetCompletionRanks.Clear();
            _pendingBonusReceives.Clear();
            _pendingStandardReceives.Clear();
            ReleaseAllViews();
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
            if (IsLayoutLocked())
            {
                FlushPendingStandardReceives();
                FlushPendingBonusReceives();
                ForceRebuildLayout();
                return;
            }

            ResetRows();
            var slotIndex = 0;

            foreach (var viewModel in _presenter.StandardCards)
            {
                var parent = GetRowTransform(slotIndex++);
                if (!_standardViews.TryGetValue(viewModel, out var view) || view == null)
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
                if (!_bonusViews.TryGetValue(viewModel, out var view) || view == null)
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

            RemoveMissingBonusViews(activeBonus);
            FlushPendingStandardReceives();
            FlushPendingBonusReceives();
            ForceRebuildLayout();
        }

        private void BeginOutgoingStandardRemovals(IReadOnlyCollection<RankStackViewModel> active)
        {
            var toRemove = new List<(RankStackViewModel ViewModel, RankStackView View)>();
            foreach (var entry in _standardViews)
            {
                var viewModel = entry.Key;
                var view = entry.Value;
                if (viewModel == null || view == null)
                {
                    toRemove.Add((viewModel, view));
                    continue;
                }

                if (active.Contains(viewModel))
                {
                    continue;
                }

                toRemove.Add((viewModel, view));
            }

            foreach (var removal in toRemove)
            {
                BeginStandardRemoval(removal.ViewModel, removal.View);
            }
        }

        private void BeginStandardRemoval(RankStackViewModel viewModel, RankStackView view)
        {
            if (viewModel == null || view == null)
            {
                FinalizeStandardRemoval(viewModel, view);
                return;
            }

            if (_removingStandard.Contains(viewModel))
            {
                return;
            }

            var rankKey = NormalizeRank(viewModel.Rank);
            if (IsTransferOutInProgress(rankKey))
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: false,
                        waitForTransferOut: true).Forget();
                }

                return;
            }

            if (ShouldWaitTransferOutStart(rankKey))
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    _removalStartGracePending.Add(rankKey);
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: true,
                        waitForSetCompletion: false,
                        waitForTransferOut: false).Forget();
                }

                return;
            }

            if (IsPendingSetCompletion(rankKey) && !view.HasCompletedSetAnimation)
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: true,
                        waitForTransferOut: false).Forget();
                }

                return;
            }

            if (view.HasActiveAnimations)
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: false,
                        waitForTransferOut: false).Forget();
                }

                return;
            }

            if (view.HasCompletedSetAnimation)
            {
                FinalizeStandardRemoval(viewModel, view);
                return;
            }

            if (!_removingStandard.Add(viewModel))
            {
                return;
            }

            if (IsTransferredRank(rankKey))
            {
                view.MarkTransferredOut();
            }

            AnimateAndDespawnStandard(viewModel, view).Forget();
        }

        private async UniTaskVoid WaitAndRetryRemovalAsync(
            RankStackViewModel viewModel,
            RankStackView view,
            bool waitForRemovalStartGrace,
            bool waitForSetCompletion,
            bool waitForTransferOut)
        {
            try
            {
                if (view != null)
                {
                    if (waitForRemovalStartGrace)
                    {
                        await UniTask.WaitUntil(() =>
                            !ShouldWaitTransferOutStart(viewModel?.Rank));
                    }

                    if (waitForTransferOut)
                    {
                        await UniTask.WaitUntil(() => !IsTransferOutInProgress(viewModel?.Rank));
                    }

                    if (waitForSetCompletion)
                    {
                        await UniTask.WaitUntil(() =>
                            view == null ||
                            view.HasCompletedSetAnimation ||
                            !IsPendingSetCompletion(viewModel?.Rank));
                    }

                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                    if (view.HasActiveAnimations)
                    {
                        await view.WaitForIdleAnimationsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var rank = NormalizeRank(viewModel?.Rank);
                Debug.LogWarning($"[PlayerHand] Wait for idle animations failed for rank '{rank}': {ex.Message}");
            }
            finally
            {
                _removalStartGracePending.Remove(NormalizeRank(viewModel?.Rank));
                _waitingStandardAnimation.Remove(viewModel);
                RenderLayout();
            }
        }

        private async UniTaskVoid AnimateAndDespawnStandard(RankStackViewModel viewModel, RankStackView view)
        {
            var rankKey = NormalizeRank(viewModel?.Rank);
            try
            {
                if (view != null)
                {
                    await view.WaitForExternalAnimationsAsync();
                    if (!view.SkipRemovalAnimation)
                    {
                        await view.PlayTransferRemovalAnimationAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerHand] Failed outgoing animation for rank '{rankKey}': {ex.Message}");
            }
            finally
            {
                FinalizeStandardRemoval(viewModel, view);
                RenderLayout();
            }
        }

        private void FinalizeStandardRemoval(RankStackViewModel viewModel, RankStackView view)
        {
            if (viewModel != null)
            {
                _standardViews.Remove(viewModel);
                _removingStandard.Remove(viewModel);
                _waitingStandardAnimation.Remove(viewModel);
                ClearTransferredRank(viewModel.Rank);
                ClearTransferOutInProgress(viewModel.Rank);
                ClearRemovalStartGrace(viewModel.Rank);
                ClearPendingSetCompletion(viewModel.Rank);
            }

            SafeDespawnStandardView(view, NormalizeRank(viewModel?.Rank));
        }

        private void OnSetCompletionAnimationFinished(RankStackView view)
        {
            if (view == null)
            {
                return;
            }

            RankStackViewModel viewModel = null;
            foreach (var entry in _standardViews)
            {
                if (ReferenceEquals(entry.Value, view))
                {
                    viewModel = entry.Key;
                    break;
                }
            }

            if (viewModel == null)
            {
                return;
            }

            if (IsViewModelActive(viewModel))
            {
                return;
            }

            if (_removingStandard.Contains(viewModel))
            {
                return;
            }

            if (IsTransferOutInProgress(viewModel.Rank) || view.HasActiveAnimations)
            {
                if (_waitingStandardAnimation.Add(viewModel))
                {
                    WaitAndRetryRemovalAsync(
                        viewModel,
                        view,
                        waitForRemovalStartGrace: false,
                        waitForSetCompletion: false,
                        waitForTransferOut: true).Forget();
                }

                return;
            }

            ClearPendingSetCompletion(viewModel.Rank);
            FinalizeStandardRemoval(viewModel, view);
            RenderLayout();
        }

        private void OnStandardViewAnimationsBecameIdle(RankStackView view)
        {
            if (view == null || !_presenter.IsActive.CurrentValue)
            {
                return;
            }

            RenderLayout();
        }

        private bool IsViewModelActive(RankStackViewModel viewModel)
        {
            if (viewModel == null)
            {
                return false;
            }

            foreach (var current in _presenter.StandardCards)
            {
                if (ReferenceEquals(current, viewModel))
                {
                    return true;
                }
            }

            return false;
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

        private void OnCardsReceived(CardsReceivedEvent evt)
        {
            if (!_presenter.IsActive.CurrentValue)
            {
                return;
            }

            DispatchStandardReceives(evt);
            DispatchBonusReceives(evt);
            FlushPendingStandardReceives();
            FlushPendingBonusReceives();
        }

        private void DispatchStandardReceives(CardsReceivedEvent evt)
        {
            if ((evt.StandardCards == null || evt.StandardCards.Count == 0) &&
                (!evt.CompletedSet || string.IsNullOrWhiteSpace(evt.CompletedSetRank)))
            {
                return;
            }

            var completedRank = NormalizeRank(evt.CompletedSetRank);
            var completedRankNotified = false;

            var grouped = (evt.StandardCards ?? Array.Empty<StandardCardData>())
                .Where(card => !string.IsNullOrWhiteSpace(card.Rank))
                .GroupBy(card => NormalizeRank(card.Rank));

            foreach (var group in grouped)
            {
                var suits = group
                    .Select(card => NormalizeSuit(card.Suit))
                    .Where(suit => !string.IsNullOrWhiteSpace(suit))
                    .ToList();

                var completedSetForRank = evt.CompletedSet &&
                                          string.Equals(group.Key, completedRank, StringComparison.OrdinalIgnoreCase);
                if (completedSetForRank &&
                    (IsTransferredRank(group.Key) || IsTransferOutInProgress(group.Key)))
                {
                    completedSetForRank = false;
                }

                if (suits.Count == 0 && !completedSetForRank)
                {
                    continue;
                }

                if (!TryGetStandardViewModel(group.Key, out var viewModel) ||
                    !TryGetStandardCardView(group.Key, out _))
                {
                    EnqueuePendingStandardReceive(
                        group.Key,
                        suits,
                        evt.Source,
                        evt.EventSeq,
                        completedSetForRank);
                    continue;
                }

                if (completedSetForRank)
                {
                    MarkPendingSetCompletion(group.Key);
                    completedRankNotified = true;
                }

                viewModel.NotifyCardsReceived(evt.Source, suits, evt.EventSeq, completedSetForRank);
            }

            if (evt.CompletedSet &&
                !completedRankNotified &&
                !string.IsNullOrWhiteSpace(completedRank))
            {
                var transferOutInProgress = IsTransferredRank(completedRank) || IsTransferOutInProgress(completedRank);
                if (transferOutInProgress)
                {
                    return;
                }

                if (TryGetStandardViewModel(completedRank, out var completedViewModel) &&
                    TryGetStandardCardView(completedRank, out _))
                {
                    MarkPendingSetCompletion(completedRank);
                    completedViewModel.NotifyCardsReceived(
                        evt.Source,
                        Array.Empty<string>(),
                        evt.EventSeq,
                        completedSet: true);
                }
                else
                {
                    EnqueuePendingStandardReceive(
                        completedRank,
                        Array.Empty<string>(),
                        evt.Source,
                        evt.EventSeq,
                        completedSet: true);
                }
            }
        }

        private void EnqueuePendingStandardReceive(
            string rank,
            IReadOnlyList<string> suits,
            string source,
            long eventSeq,
            bool completedSet)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return;
            }

            var now = Time.unscaledTime;
            if (suits != null && suits.Count > 0)
            {
                foreach (var suit in suits)
                {
                    var suitKey = NormalizeSuit(suit);
                    if (string.IsNullOrWhiteSpace(suitKey))
                    {
                        continue;
                    }

                    _pendingStandardReceives.Add(new PendingStandardReceive(
                        rankKey,
                        suitKey,
                        source,
                        eventSeq,
                        completedSet,
                        now));
                }

                return;
            }

            _pendingStandardReceives.Add(new PendingStandardReceive(
                rankKey,
                string.Empty,
                source,
                eventSeq,
                completedSet,
                now));
        }

        private void FlushPendingStandardReceives()
        {
            if (_pendingStandardReceives.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            _pendingStandardReceives.RemoveAll(item => now - item.CreatedAt > PendingStandardReceiveMaxAgeSeconds);
            if (_pendingStandardReceives.Count == 0)
            {
                return;
            }

            var grouped = _pendingStandardReceives
                .GroupBy(item => new { item.Rank, item.Source, item.EventSeq, item.CompletedSet })
                .ToList();

            var consumed = new HashSet<PendingStandardReceive>();
            foreach (var group in grouped)
            {
                if (!TryGetStandardViewModel(group.Key.Rank, out var viewModel) ||
                    !TryGetStandardCardView(group.Key.Rank, out _))
                {
                    continue;
                }

                var suits = group
                    .Select(item => item.Suit)
                    .Where(suit => !string.IsNullOrWhiteSpace(suit))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var completedSetForRank = group.Key.CompletedSet;
                if (completedSetForRank &&
                    (IsTransferredRank(group.Key.Rank) || IsTransferOutInProgress(group.Key.Rank)))
                {
                    completedSetForRank = false;
                }

                if (completedSetForRank)
                {
                    MarkPendingSetCompletion(group.Key.Rank);
                }

                viewModel.NotifyCardsReceived(
                    group.Key.Source,
                    suits,
                    group.Key.EventSeq,
                    completedSetForRank);

                foreach (var item in group)
                {
                    consumed.Add(item);
                }
            }

            if (consumed.Count == 0)
            {
                return;
            }

            _pendingStandardReceives.RemoveAll(consumed.Contains);
        }

        private IReadOnlyList<string> GetPendingStandardSuitsForRank(string rank)
        {
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey) || _pendingStandardReceives.Count == 0)
            {
                return Array.Empty<string>();
            }

            return _pendingStandardReceives
                .Where(item => string.Equals(item.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Suit)
                .Where(suit => !string.IsNullOrWhiteSpace(suit))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void DispatchBonusReceives(CardsReceivedEvent evt)
        {
            if (evt.BonusCards == null || evt.BonusCards.Count == 0)
            {
                return;
            }

            foreach (var card in evt.BonusCards)
            {
                var origin = ResolveReceiveOrigin(evt.Source);
                if (origin == null || !TryAnimateBonusReceive(card, origin))
                {
                    EnqueuePendingBonusReceive(card.BonusType, evt.Source);
                }
            }
        }

        private void EnqueuePendingBonusReceive(string bonusType, string source)
        {
            if (string.IsNullOrWhiteSpace(bonusType))
            {
                return;
            }

            _pendingBonusReceives.Enqueue(new PendingBonusReceive(
                NormalizeBonus(bonusType),
                source,
                Time.unscaledTime));
        }

        private void FlushPendingBonusReceives()
        {
            if (_pendingBonusReceives.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var pendingCount = _pendingBonusReceives.Count;
            for (var i = 0; i < pendingCount; i++)
            {
                var pending = _pendingBonusReceives.Dequeue();
                if (now - pending.CreatedAt > PendingBonusReceiveMaxAgeSeconds)
                {
                    continue;
                }

                var origin = ResolveReceiveOrigin(pending.Source);
                if (origin == null ||
                    !TryAnimateBonusReceive(new BonusCardData(pending.BonusType), origin))
                {
                    _pendingBonusReceives.Enqueue(pending);
                }
            }
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

                if (_bonusViews.TryGetValue(viewModel, out var view) && view != null)
                {
                    view.PlayReceiveFromAsync(origin).Forget();
                    return true;
                }
            }

            return false;
        }

        private bool TryGetStandardViewModel(string rank, out RankStackViewModel viewModel)
        {
            viewModel = null;
            var rankKey = NormalizeRank(rank);
            if (string.IsNullOrWhiteSpace(rankKey))
            {
                return false;
            }

            foreach (var candidate in _presenter.StandardCards)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.Rank, rankKey, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                viewModel = candidate;
                return true;
            }

            return false;
        }

        private RectTransform ResolveReceiveOrigin(string source)
        {
            if (string.Equals(source, "deck", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveDeckOrigin();
            }

            return ResolveOpponentHandAnchor();
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

            var animator = _cardTransferAnimator ?? FindObjectOfType<CardTransferAnimator>();
            _cachedOpponentAnchor = animator != null ? animator.OpponentHandAnchor : null;
            return _cachedOpponentAnchor;
        }

        private void ResetRows()
        {
            foreach (var row in _rows)
            {
                row.Count = 0;
                row.Root.gameObject.SetActive(false);
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
            view.ConfigureReceiveOrigins(ResolveDeckOrigin, ResolveOpponentHandAnchor);
            var pendingSuits = GetPendingStandardSuitsForRank(viewModel.Rank);
            if (pendingSuits.Count > 0)
            {
                view.SetPendingReceiveSuits(pendingSuits);
            }
            view.SetCompletionAnimationFinished += OnSetCompletionAnimationFinished;
            view.AnimationsBecameIdle += OnStandardViewAnimationsBecameIdle;
            view.Initialize(viewModel).Forget();
            return view;
        }

        private void ReleaseAllViews()
        {
            foreach (var view in _standardViews.Values)
            {
                SafeDespawnStandardView(view, string.Empty);
            }

            _standardViews.Clear();
            _removingStandard.Clear();
            _waitingStandardAnimation.Clear();
            _transferredRanks.Clear();
            _activeTransferOutByRank.Clear();
            _transferOutReleaseAt.Clear();
            _removalStartGraceUntil.Clear();
            _removalStartGracePending.Clear();
            _pendingSetCompletionRanks.Clear();
            _pendingBonusReceives.Clear();
            _pendingStandardReceives.Clear();

            foreach (var view in _bonusViews.Values)
            {
                SafeDespawnBonusView(view);
            }

            _bonusViews.Clear();
        }

        private void SafeDespawnStandardView(RankStackView view, string rankKey)
        {
            if (view == null)
            {
                return;
            }

            try
            {
                view.SetCompletionAnimationFinished -= OnSetCompletionAnimationFinished;
                view.AnimationsBecameIdle -= OnStandardViewAnimationsBecameIdle;
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

        private void ForceRebuildLayout()
        {
            if (_rowsRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rowsRoot);
        }

        private bool IsTransferredRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            return _transferredRanks.Contains(NormalizeRank(rank));
        }

        private void ClearTransferredRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            _transferredRanks.Remove(NormalizeRank(rank));
        }

        private bool IsTransferOutInProgress(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            CleanupTransferOutReleaseFlags();
            CleanupRemovalStartGraceFlags();
            var rankKey = NormalizeRank(rank);
            if (_activeTransferOutByRank.TryGetValue(rankKey, out var count) && count > 0)
            {
                return true;
            }

            if (_transferOutReleaseAt.TryGetValue(rankKey, out var releaseAt))
            {
                return Time.unscaledTime < releaseAt;
            }

            return false;
        }

        private void ClearTransferOutInProgress(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            _activeTransferOutByRank.Remove(rankKey);
            _transferOutReleaseAt.Remove(rankKey);
            ClearRemovalStartGrace(rankKey);
        }

        private void CleanupTransferOutReleaseFlags()
        {
            if (_transferOutReleaseAt.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var expired = _transferOutReleaseAt
                .Where(pair => now >= pair.Value)
                .Select(pair => pair.Key)
                .ToList();
            foreach (var rank in expired)
            {
                _transferOutReleaseAt.Remove(rank);
            }
        }

        private bool ShouldWaitTransferOutStart(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            CleanupRemovalStartGraceFlags();

            var rankKey = NormalizeRank(rank);
            if (IsTransferOutInProgress(rankKey) || IsTransferredRank(rankKey) || IsPendingSetCompletion(rankKey))
            {
                ClearRemovalStartGrace(rankKey);
                return false;
            }

            if (!_removalStartGraceUntil.TryGetValue(rankKey, out var graceUntil))
            {
                graceUntil = Time.unscaledTime + RemovalStartGraceSeconds;
                _removalStartGraceUntil[rankKey] = graceUntil;
            }

            if (Time.unscaledTime < graceUntil)
            {
                return true;
            }

            ClearRemovalStartGrace(rankKey);
            return false;
        }

        private void ClearRemovalStartGrace(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            _removalStartGraceUntil.Remove(rankKey);
            _removalStartGracePending.Remove(rankKey);
        }

        private void CleanupRemovalStartGraceFlags()
        {
            if (_removalStartGraceUntil.Count == 0)
            {
                return;
            }

            var now = Time.unscaledTime;
            var expired = _removalStartGraceUntil
                .Where(pair => now >= pair.Value)
                .Select(pair => pair.Key)
                .ToList();
            foreach (var rank in expired)
            {
                _removalStartGraceUntil.Remove(rank);
                _removalStartGracePending.Remove(rank);
            }
        }

        private bool IsLayoutLocked()
        {
            CleanupTransferOutReleaseFlags();
            CleanupRemovalStartGraceFlags();
            if (_removingStandard.Count > 0 ||
                _waitingStandardAnimation.Count > 0 ||
                _activeTransferOutByRank.Count > 0 ||
                _transferOutReleaseAt.Count > 0 ||
                _removalStartGracePending.Count > 0 ||
                _pendingSetCompletionRanks.Count > 0)
            {
                return true;
            }

            foreach (var view in _standardViews.Values)
            {
                if (view != null && view.HasActiveAnimations)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPendingSetCompletion(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return false;
            }

            return _pendingSetCompletionRanks.Contains(NormalizeRank(rank));
        }

        private void MarkPendingSetCompletion(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            _pendingSetCompletionRanks.Add(NormalizeRank(rank));
        }

        private void ClearPendingSetCompletion(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            _pendingSetCompletionRanks.Remove(NormalizeRank(rank));
        }

        public void MarkRankTransferredOut(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            _transferredRanks.Add(rankKey);
            _transferOutReleaseAt.Remove(rankKey);
            ClearRemovalStartGrace(rankKey);
            if (_activeTransferOutByRank.TryGetValue(rankKey, out var count))
            {
                _activeTransferOutByRank[rankKey] = count + 1;
            }
            else
            {
                _activeTransferOutByRank[rankKey] = 1;
            }
        }

        public void CompleteRankTransfer(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return;
            }

            var rankKey = NormalizeRank(rank);
            if (_activeTransferOutByRank.TryGetValue(rankKey, out var count))
            {
                count--;
                if (count > 0)
                {
                    _activeTransferOutByRank[rankKey] = count;
                }
                else
                {
                    _activeTransferOutByRank.Remove(rankKey);
                    _transferOutReleaseAt[rankKey] = Time.unscaledTime + TransferOutReleaseDelaySeconds;
                }
            }
            else
            {
                _transferOutReleaseAt[rankKey] = Time.unscaledTime + TransferOutReleaseDelaySeconds;
            }

            RenderLayout();
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
