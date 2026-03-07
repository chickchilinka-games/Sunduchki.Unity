using System;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using R3;

namespace Features.PlayerHandSystemImpl.Presentation.Presenters
{
    public sealed class PlayerHandStoreResetOnDeactivate : IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly PlayerHandStateStoreApplier _stateStoreApplier;
        private readonly CompositeDisposable _subscriptions = new();
        private bool _started;

        public PlayerHandStoreResetOnDeactivate(
            PlayerHandPresentationContext context,
            PlayerHandStateStoreApplier stateStoreApplier)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _stateStoreApplier = stateStoreApplier ?? throw new ArgumentNullException(nameof(stateStoreApplier));
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _context.IsActive
                .Subscribe(active =>
                {
                    if (!active)
                    {
                        _stateStoreApplier.Clear();
                    }
                })
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
