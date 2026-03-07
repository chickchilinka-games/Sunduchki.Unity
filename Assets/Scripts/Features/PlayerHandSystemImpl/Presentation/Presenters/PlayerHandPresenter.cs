using System;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Features.PlayerHandSystemImpl.Presentation.ViewModel;
using Modules.PlayerHand.Data;
using ObservableCollections;
using R3;
using Zenject;

namespace Features.PlayerHandSystemImpl.Presentation.Presenters
{
    /// <summary>
    /// Presenter that owns player-hand view-models and reacts to domain events.
    /// </summary>
    public sealed class PlayerHandPresenter : IInitializable, IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly RankStackViewModelStorage _storage;
        private readonly PlayerHandStoreResetOnDeactivate _storeResetOnDeactivate;
        private readonly PlayerHandStateStoreApplier _stateStoreApplier;
        private readonly PlayerHandStoreChangePublisher _storeChangePublisher;

        public PlayerHandPresenter(
            PlayerHandPresentationContext context,
            RankStackViewModelStorage storage,
            PlayerHandStoreResetOnDeactivate storeResetOnDeactivate,
            PlayerHandStateStoreApplier stateStoreApplier,
            PlayerHandStoreChangePublisher storeChangePublisher)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _storeResetOnDeactivate = storeResetOnDeactivate ?? throw new ArgumentNullException(nameof(storeResetOnDeactivate));
            _stateStoreApplier = stateStoreApplier ?? throw new ArgumentNullException(nameof(stateStoreApplier));
            _storeChangePublisher = storeChangePublisher ?? throw new ArgumentNullException(nameof(storeChangePublisher));
        }

        public ReadOnlyReactiveProperty<bool> IsActive => _context.IsActive;
        public string LocalPlayerId => _context.LocalPlayerId.CurrentValue;
        public IReadOnlyObservableList<RankStackViewModel> StandardCards => _storage.Items;
        public Observable<Unit> HandChanged => _storeChangePublisher.HandChanged;

        public void Initialize()
        {
            _storeResetOnDeactivate.Start();
            _storeChangePublisher.Start();
        }

        public void Dispose()
        {
            _storeResetOnDeactivate.Dispose();
            _storeChangePublisher.Dispose();
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

            _stateStoreApplier.Apply(state);
        }

    }
}
