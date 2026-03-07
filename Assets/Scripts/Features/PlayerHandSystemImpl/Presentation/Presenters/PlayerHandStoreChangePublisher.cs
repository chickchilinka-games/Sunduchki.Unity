using System;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using R3;

namespace Features.PlayerHandSystemImpl.Presentation.Presenters
{
    public sealed class PlayerHandStoreChangePublisher : IDisposable
    {
        private readonly RankStackViewModelStorage _storage;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly Subject<Unit> _handChanged = new();
        private bool _started;

        public PlayerHandStoreChangePublisher(RankStackViewModelStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public Observable<Unit> HandChanged => _handChanged;

        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            // Keep view refresh in sync with any store mutation (not only snapshots).
            _storage.Changed
                .Subscribe(_ => _handChanged.OnNext(Unit.Default))
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _handChanged.Dispose();
        }
    }
}
