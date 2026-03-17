using System;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Presenters
{
    public sealed class PlayerHandPresenter : IInitializable, IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly RankStackViewModelStorage _storage;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly Subject<Unit> _handChanged = new();

        public PlayerHandPresenter(
            PlayerHandPresentationContext context,
            RankStackViewModelStorage storage)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public ReadOnlyReactiveProperty<bool> IsActive => _context.IsActive;
        public string LocalPlayerId => _context.LocalPlayerId.CurrentValue;
        public IReadOnlyObservableList<RankStackViewModel> StandardCards => _storage.Items;
        public Observable<Unit> HandChanged => _handChanged;

        public void Initialize()
        {
            _context.IsActive
                .Subscribe(active =>
                {
                    if (!active)
                    {
                        _storage.Clear();
                    }
                })
                .AddTo(_subscriptions);

            _storage.Changed
                .Subscribe(_ => _handChanged.OnNext(Unit.Default))
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _handChanged.Dispose();
            _storage.Dispose();
        }

        public void SetLocalPlayerId(string localPlayerId)
        {
            _context.SetLocalPlayerId(localPlayerId);
        }

        public void OnHandUpdated(PlayerHandState state)
        {
            if (!_context.IsActive.CurrentValue)
            {
                return;
            }

            _storage.Sync(state.StandardCards);
        }

    }
}
