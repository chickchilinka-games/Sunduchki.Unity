using System;
using R3;

namespace Features.PlayerHandSystemImpl.Presentation.Storage
{
    public sealed class PlayerHandPresentationContext : IDisposable
    {
        private readonly ReactiveProperty<bool> _isActive = new(false);
        private readonly ReactiveProperty<string> _localPlayerId = new(string.Empty);
        private readonly ReactiveProperty<bool> _isAwaitingAsk = new(false);

        public ReadOnlyReactiveProperty<bool> IsActive => _isActive;
        public ReadOnlyReactiveProperty<string> LocalPlayerId => _localPlayerId;
        public ReadOnlyReactiveProperty<bool> IsAwaitingAsk => _isAwaitingAsk;

        public void SetActive(bool isActive)
        {
            if (_isActive.Value == isActive)
            {
                return;
            }

            _isActive.Value = isActive;
        }

        public void SetLocalPlayerId(string localPlayerId)
        {
            if (string.IsNullOrWhiteSpace(localPlayerId))
            {
                return;
            }

            if (string.Equals(_localPlayerId.Value, localPlayerId, StringComparison.Ordinal))
            {
                return;
            }

            _localPlayerId.Value = localPlayerId;
        }

        public void SetAwaitingAsk(bool isAwaiting)
        {
            if (_isAwaitingAsk.Value == isAwaiting)
            {
                return;
            }

            _isAwaitingAsk.Value = isAwaiting;
        }

        public void Clear()
        {
            _isActive.Value = false;
            _localPlayerId.Value = string.Empty;
            _isAwaitingAsk.Value = false;
        }

        public void Dispose()
        {
            _isActive.Dispose();
            _localPlayerId.Dispose();
            _isAwaitingAsk.Dispose();
        }
    }
}
