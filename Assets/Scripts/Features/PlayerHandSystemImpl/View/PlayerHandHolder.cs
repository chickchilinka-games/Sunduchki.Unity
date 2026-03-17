using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.CardRequestSystemImpl.View;
using Features.PlayerHandSystemImpl.Bootstrap;
using Features.PlayerHandSystemImpl.Factory;
using Features.PlayerHandSystemImpl.Presentation.Presenters;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Modules.PlayerHand.Data;
using R3;
using UnityEngine;
using Zenject;

namespace Features.PlayerHandSystemImpl.View
{
    public partial class PlayerHandHolder : MonoBehaviour
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
        private BonusCardViewModelStorage _bonusStorage;
        private DiContainer _container;
        private RankStackView _standardViewPrefab;
        private BonusCardViewPool _bonusPool;
        private CancellationTokenSource _renderLoopCts;
        private bool _isRenderScheduled;
        private bool _isRenderRunning;

        private string _localPlayerId;
        private CompositeDisposable _subscriptions;

        private readonly Dictionary<RankStackViewModel, RankStackView> _standardViews = new();
        private readonly Dictionary<BonusCardViewModel, BonusCardView> _bonusViews = new();
        private readonly HashSet<RankStackViewModel> _pendingStandardRemoval = new();
        private readonly Dictionary<RankStackViewModel, float> _pendingStandardRemovalSince = new();
        private readonly Queue<Action> _pendingUiMutations = new();
        private readonly Queue<PendingBonusReceive> _pendingBonusReceives = new();
        private readonly Queue<PendingTransferOut> _pendingTransferOuts = new();
        private bool _isExecutingUiMutations;
        private HandLayoutController _layoutController;

        private RectTransform _cachedOpponentAnchor;
        private bool _warnedMissingDeckOrigin;
        private const float PendingBonusReceiveMaxAgeSeconds = 8f;
        private const float PendingTransferOutMaxAgeSeconds = 4f;
        private const float MissingStandardRemovalGraceSeconds = 0.35f;
        private const int MaxUiMutationsPerPass = 256;

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

        private readonly struct PendingTransferOut
        {
            public string Rank { get; }
            public IReadOnlyList<string> Suits { get; }
            public int Count { get; }
            public string ActionId { get; }
            public float CreatedAt { get; }

            public PendingTransferOut(string rank, IReadOnlyList<string> suits, int count, string actionId, float createdAt)
            {
                Rank = rank ?? string.Empty;
                Suits = suits ?? Array.Empty<string>();
                Count = count;
                ActionId = actionId ?? string.Empty;
                CreatedAt = createdAt;
            }
        }

        [Inject]
        public void Construct(
            PlayerHandPresenter presenter,
            BonusHandPresenter bonusPresenter,
            CardRequestPresentationPresenter cardRequestPresenter,
            BonusCardViewModelStorage bonusStorage,
            DiContainer container,
            [Inject(Id = PlayerHandSystemMonoInstaller.StandardCardViewPrefabBindingId)] RankStackView standardViewPrefab,
            BonusCardViewPool bonusPool)
        {
            _presenter = presenter;
            _bonusPresenter = bonusPresenter;
            _cardRequestPresenter = cardRequestPresenter;
            _bonusStorage = bonusStorage;
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

            _layoutController ??= new HandLayoutController(_maxItemsPerRow, _rowItemSpacing, _rowAlignment);
            _layoutController.SetRoot(_rowsRoot);
            _renderLoopCts = new CancellationTokenSource();
            _isRenderScheduled = false;
            _isRenderRunning = false;
            _subscriptions = new CompositeDisposable();

            _presenter.IsActive
                .Subscribe(active =>
                {
                    if (active)
                    {
                        _localPlayerId = _presenter.LocalPlayerId;
                        RequestLayoutRefresh();
                    }
                    else
                    {
                        ReleaseAllViews();
                    }
                })
                .AddTo(_subscriptions);

            _presenter.HandChanged
                .Subscribe(_ => RequestLayoutRefresh())
                .AddTo(_subscriptions);

            _bonusStorage.Changed
                .Subscribe(_ => RequestLayoutRefresh())
                .AddTo(_subscriptions);

            _cardRequestPresenter.CardTransferred
                .Subscribe(OnCardTransferred)
                .AddTo(_subscriptions);

            _cardRequestPresenter.CardsReceived
                .Subscribe(OnCardsReceived)
                .AddTo(_subscriptions);

            _localPlayerId = _presenter.LocalPlayerId;
            if (_presenter.IsActive.CurrentValue)
            {
                RequestLayoutRefresh();
            }
        }

        private void OnDisable()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;

            _renderLoopCts?.Cancel();
            _renderLoopCts?.Dispose();
            _renderLoopCts = null;
            _isRenderScheduled = false;
            _isRenderRunning = false;
            _pendingStandardRemoval.Clear();
            _pendingStandardRemovalSince.Clear();
            _pendingUiMutations.Clear();
            _pendingBonusReceives.Clear();
            _pendingTransferOuts.Clear();
            _isExecutingUiMutations = false;
            ReleaseAllViews();
        }

        private void RequestLayoutRefresh()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            _isRenderScheduled = true;
            if (_isRenderRunning)
            {
                return;
            }

            RunRenderLoopAsync(_renderLoopCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTaskVoid RunRenderLoopAsync(CancellationToken token)
        {
            _isRenderRunning = true;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!isActiveAndEnabled)
                    {
                        break;
                    }

                    if (!_isRenderScheduled)
                    {
                        break;
                    }

                    _isRenderScheduled = false;
                    RenderLayout();

                    if (!_isRenderScheduled)
                    {
                        break;
                    }

                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerHand] Render loop failed: {ex.Message}");
            }
            finally
            {
                _isRenderRunning = false;
            }
        }

        private void EnqueueUiMutation(Action mutation)
        {
            if (mutation == null || !_presenter.IsActive.CurrentValue)
            {
                return;
            }

            _pendingUiMutations.Enqueue(mutation);
            RequestLayoutRefresh();
        }

        private void ExecutePendingUiMutations()
        {
            if (_isExecutingUiMutations || _pendingUiMutations.Count == 0)
            {
                return;
            }

            _isExecutingUiMutations = true;
            try
            {
                var processed = 0;
                while (_pendingUiMutations.Count > 0 && processed < MaxUiMutationsPerPass)
                {
                    var mutation = _pendingUiMutations.Dequeue();
                    processed++;
                    try
                    {
                        mutation?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[PlayerHand] UI mutation failed: {ex.Message}");
                    }
                }

                if (_pendingUiMutations.Count > 0)
                {
                    RequestLayoutRefresh();
                }
            }
            finally
            {
                _isExecutingUiMutations = false;
            }
        }

        private void RenderLayout()
        {
            if (!_presenter.IsActive.CurrentValue)
            {
                _pendingUiMutations.Clear();
                ReleaseAllViews();
                return;
            }

            ExecutePendingUiMutations();

            var presenterStandard = _presenter.StandardCards.ToList();
            var presenterBonus = _bonusPresenter.BonusCards.ToList();
            var activeStandard = new HashSet<RankStackViewModel>(presenterStandard);
            var activeBonus = new HashSet<BonusCardViewModel>(presenterBonus);

            MarkMissingStandardForRemoval(activeStandard);
            var orderedStandard = BuildStandardLayoutOrder(activeStandard, presenterStandard);

            ResetRows();
            var slotIndex = 0;

            foreach (var viewModel in orderedStandard)
            {
                var parent = GetRowTransform(slotIndex++);
                if (!_standardViews.TryGetValue(viewModel, out var view) || view == null)
                {
                    if (!activeStandard.Contains(viewModel))
                    {
                        continue;
                    }

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

            foreach (var viewModel in presenterBonus)
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
            FlushPendingTransferOuts();
            FlushPendingBonusReceives();
            ForceRebuildLayout();
        }

        private void MarkMissingStandardForRemoval(IReadOnlyCollection<RankStackViewModel> activeStandard)
        {
            var now = Time.unscaledTime;
            var entries = _standardViews.ToArray();
            foreach (var entry in entries)
            {
                var viewModel = entry.Key;
                var view = entry.Value;
                if (viewModel == null || view == null)
                {
                    continue;
                }

                if (activeStandard.Contains(viewModel))
                {
                    _pendingStandardRemoval.Remove(viewModel);
                    _pendingStandardRemovalSince.Remove(viewModel);
                    continue;
                }

                if (_pendingStandardRemoval.Add(viewModel))
                {
                    _pendingStandardRemovalSince[viewModel] = now;
                }

                if (!_pendingStandardRemovalSince.TryGetValue(viewModel, out var pendingSince))
                {
                    pendingSince = now;
                    _pendingStandardRemovalSince[viewModel] = pendingSince;
                }

                if (now - pendingSince < MissingStandardRemovalGraceSeconds)
                {
                    continue;
                }

                view.RequestRemoval();
            }
        }

        private void OnStandardViewRemovalReady(RankStackView view)
        {
            if (view == null || !_presenter.IsActive.CurrentValue)
            {
                return;
            }

            if (!TryGetViewModelByView(view, out var viewModel))
            {
                return;
            }

            if (IsViewModelActive(viewModel) && !_pendingStandardRemoval.Contains(viewModel))
            {
                return;
            }

            FinalizeStandardRemoval(viewModel, view);
            RequestLayoutRefresh();
        }

        private void FinalizeStandardRemoval(RankStackViewModel viewModel, RankStackView view)
        {
            if (viewModel != null)
            {
                _standardViews.Remove(viewModel);
                _pendingStandardRemoval.Remove(viewModel);
                _pendingStandardRemovalSince.Remove(viewModel);
            }

            SafeDespawnStandardView(view, NormalizeRank(viewModel?.Rank));
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

        private static CardsReceivedEvent SnapshotCardsReceivedEvent(CardsReceivedEvent evt)
        {
            var standardCards = evt.StandardCards != null
                ? evt.StandardCards.ToArray()
                : Array.Empty<StandardCardData>();
            var bonusCards = evt.BonusCards != null
                ? evt.BonusCards.ToArray()
                : Array.Empty<BonusCardData>();

            return new CardsReceivedEvent(
                evt.PlayerId,
                evt.Source,
                standardCards,
                bonusCards,
                evt.EventSeq,
                evt.CompletedSet,
                evt.CompletedSetRank);
        }
    }
}
