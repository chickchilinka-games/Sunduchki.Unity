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
using Modules.CardRequestSystem.Data;
using Modules.PlayerHand.Data;
using R3;
using UnityEngine;
using UnityEngine.UI;
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
        private readonly HashSet<RankStackViewModel> _removingStandard = new();
        private readonly HashSet<RankStackViewModel> _waitingStandardAnimation = new();
        private HandLayoutController _layoutController;
        private HandAnimationGate _animationGate;
        private HandReceiveBuffer _receiveBuffer;
        private readonly Queue<Action> _pendingUiMutations = new();
        private bool _isExecutingUiMutations;

        private RectTransform _cachedOpponentAnchor;
        private bool _warnedMissingDeckOrigin;
        private const float PendingBonusReceiveMaxAgeSeconds = 8f;
        private const float PendingStandardReceiveMaxAgeSeconds = 8f;
        private const float TransferOutReleaseDelaySeconds = 0.2f;
        private const float RemovalStartGraceSeconds = 0.2f;
        private const int MaxUiMutationsPerPass = 256;

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
            _animationGate ??= new HandAnimationGate(TransferOutReleaseDelaySeconds, RemovalStartGraceSeconds);
            _receiveBuffer ??= new HandReceiveBuffer(PendingStandardReceiveMaxAgeSeconds, PendingBonusReceiveMaxAgeSeconds);
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

            _cardRequestPresenter.CardRequested
                .Subscribe(OnCardRequested)
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
            _animationGate?.Clear();
            _receiveBuffer?.Clear();
            _removingStandard.Clear();
            _waitingStandardAnimation.Clear();
            _pendingUiMutations.Clear();
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

        private static CardRequestEvent SnapshotCardRequestEvent(CardRequestEvent evt)
        {
            var cards = evt.Cards != null
                ? evt.Cards.ToArray()
                : Array.Empty<CardTransferCardData>();

            return new CardRequestEvent(
                evt.EventType,
                evt.AskerId,
                evt.TargetId,
                evt.Rank,
                evt.Count,
                cards,
                evt.Timestamp);
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
