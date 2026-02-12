using System;
using Features.PlayerHandSystemImpl.Storage;
using Features.PlayerHandSystemImpl.ViewModel;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presenters
{
    /// <summary>
    /// Presenter that owns player-hand view-models and reacts to domain events.
    /// </summary>
    public sealed class PlayerHandPresenter : IInitializable, IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly RankStackViewModelStore _store;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly Subject<Unit> _handChanged = new();

        public PlayerHandPresenter(
            PlayerHandPresentationContext context,
            RankStackViewModelStore store)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public ReadOnlyReactiveProperty<bool> IsActive => _context.IsActive;
        public string LocalPlayerId => _context.LocalPlayerId.CurrentValue;
        public IReadOnlyObservableList<RankStackViewModel> StandardCards => _store.Items;
        public Observable<Unit> HandChanged => _handChanged;

        public void Initialize()
        {
            _context.IsActive
                .Subscribe(active =>
                {
                    if (!active)
                    {
                        ClearViewModels();
                    }
                })
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _handChanged.Dispose();
            _store.Dispose();
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

            UpdateHand(state);
        }

        private void UpdateHand(PlayerHandState state)
        {
            _store.Sync(state.StandardCards);
            _handChanged.OnNext(Unit.Default);
        }

        private void ClearViewModels()
        {
            _store.Clear();
            _handChanged.OnNext(Unit.Default);
        }

    }
}
