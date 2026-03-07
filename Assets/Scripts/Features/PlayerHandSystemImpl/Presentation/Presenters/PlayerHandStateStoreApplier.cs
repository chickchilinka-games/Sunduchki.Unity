using System;
using Features.PlayerHandSystemImpl.Presentation.Storage;
using Modules.PlayerHand.Data;

namespace Features.PlayerHandSystemImpl.Presentation.Presenters
{
    public sealed class PlayerHandStateStoreApplier
    {
        private readonly RankStackViewModelStorage _storage;

        public PlayerHandStateStoreApplier(RankStackViewModelStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void Apply(PlayerHandState state)
        {
            _storage.Sync(state.StandardCards);
        }

        public void Clear()
        {
            _storage.Clear();
        }
    }
}
