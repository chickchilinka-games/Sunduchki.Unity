using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.PlayerHandSystemImpl.Presentation.Presenters;
using Modules.AssetSystem.Services;
using R3;
using UnityEngine;
using Zenject;

namespace Features.PlayerHandSystemImpl.View3D
{
    public sealed class BoardSurface3DView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] private Transform _rankSetsRoot;
        [SerializeField] private Transform _bonusSetsRoot;

        [Header("Prefabs")]
        [SerializeField] private RankCardSet3DView _rankSetPrefab;
        [SerializeField] private BonusCardSet3DView _bonusSetPrefab;

        [Header("Rank Layout")]
        [SerializeField, Min(1)] private int _rankSetsPerRow = 4;
        [SerializeField] private float _rankSetSpacingX = 1.7f;
        [SerializeField] private float _rankSetSpacingZ = -1.15f;

        [Header("Bonus Layout")]
        [SerializeField, Min(1)] private int _bonusSetsPerRow = 4;
        [SerializeField] private float _bonusSetSpacingX = 1.5f;
        [SerializeField] private float _bonusSetSpacingZ = -1.0f;

        private PlayerHandPresenter _playerHandPresenter;
        private BonusHandPresenter _bonusHandPresenter;
        private AssetService _assetService;

        private BoardCardSpriteResolver _spriteResolver;
        private BoardSurfaceSnapshotBuilder _snapshotBuilder;
        private BoardSetViewRegistry _setViewRegistry;
        private CompositeDisposable _subscriptions;
        private CancellationTokenSource _refreshCts;
        private int _refreshRevision;

        [Inject]
        public void Construct(
            PlayerHandPresenter playerHandPresenter,
            BonusHandPresenter bonusHandPresenter,
            AssetService assetService)
        {
            _playerHandPresenter = playerHandPresenter ?? throw new ArgumentNullException(nameof(playerHandPresenter));
            _bonusHandPresenter = bonusHandPresenter ?? throw new ArgumentNullException(nameof(bonusHandPresenter));
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        }

        private void Awake()
        {
            if (_rankSetsRoot == null)
            {
                _rankSetsRoot = transform;
            }

            if (_bonusSetsRoot == null)
            {
                _bonusSetsRoot = transform;
            }
        }

        private void OnEnable()
        {
            _spriteResolver ??= new BoardCardSpriteResolver(_assetService);
            _snapshotBuilder ??= new BoardSurfaceSnapshotBuilder();
            _setViewRegistry ??= new BoardSetViewRegistry(
                _rankSetsRoot,
                _bonusSetsRoot,
                _rankSetPrefab,
                _bonusSetPrefab);

            _subscriptions?.Dispose();
            _subscriptions = new CompositeDisposable();

            _playerHandPresenter.IsActive
                .Subscribe(active =>
                {
                    if (!active)
                    {
                        CancelRefresh();
                        _setViewRegistry?.Clear();
                        return;
                    }

                    RequestRefresh();
                })
                .AddTo(_subscriptions);

            _playerHandPresenter.HandChanged
                .Subscribe(_ => RequestRefresh())
                .AddTo(_subscriptions);

            _bonusHandPresenter.HandChanged
                .Subscribe(_ => RequestRefresh())
                .AddTo(_subscriptions);

            RequestRefresh();
        }

        private void OnDisable()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
            CancelRefresh();
            _setViewRegistry?.Clear();
        }

        private void RequestRefresh()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            CancelRefresh();
            _refreshRevision++;
            _refreshCts = new CancellationTokenSource();
            RefreshAsync(_refreshRevision, _refreshCts.Token).Forget();
        }

        private async UniTaskVoid RefreshAsync(int revision, CancellationToken cancellationToken)
        {
            if (_playerHandPresenter == null || _bonusHandPresenter == null)
            {
                return;
            }

            if (!_playerHandPresenter.IsActive.CurrentValue)
            {
                _setViewRegistry?.Clear();
                return;
            }

            var snapshot = _snapshotBuilder.Build(_playerHandPresenter, _bonusHandPresenter);

            await _setViewRegistry.SyncRankSetsAsync(
                snapshot.RankSets,
                _spriteResolver,
                _rankSetsPerRow,
                _rankSetSpacingX,
                _rankSetSpacingZ,
                cancellationToken);
            if (IsRefreshExpired(revision, cancellationToken))
            {
                return;
            }

            await _setViewRegistry.SyncBonusSetsAsync(
                snapshot.BonusSets,
                _spriteResolver,
                _bonusSetsPerRow,
                _bonusSetSpacingX,
                _bonusSetSpacingZ,
                cancellationToken);
        }

        private bool IsRefreshExpired(int revision, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested || revision != _refreshRevision;
        }

        private void CancelRefresh()
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = null;
        }
    }
}
