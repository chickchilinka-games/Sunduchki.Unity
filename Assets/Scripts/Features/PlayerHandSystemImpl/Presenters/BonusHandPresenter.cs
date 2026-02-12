using System;
using System.Collections.Generic;
using Features.PlayerHandSystemImpl.Storage;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.BonusSystem.Data;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presenters
{
    public sealed class BonusHandPresenter : IInitializable, IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly BonusCardViewModelStore _store;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly Subject<Unit> _handChanged = new();

        public IReadOnlyObservableList<BonusCardViewModel> BonusCards => _store.Items;
        public Observable<Unit> HandChanged => _handChanged;

        public BonusHandPresenter(
            PlayerHandPresentationContext context,
            BonusCardViewModelStore store)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public void Initialize()
        {
            _context.IsActive
                .Subscribe(active =>
                {
                    if (!active)
                    {
                        _store.Clear();
                    }
                })
                .AddTo(_subscriptions);

            _store.Changed
                .Subscribe(_ => _handChanged.OnNext(Unit.Default))
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _handChanged.Dispose();
            _store.Dispose();
        }

        public void SyncBonusCards(IReadOnlyList<BonusCardData> cards)
        {
            if (!_context.IsActive.CurrentValue)
            {
                return;
            }

            _store.Sync(cards);
        }
    }
}
