using System;

namespace Features.PlayerHandSystemImpl.Utils
{
    internal sealed class ExternalAnimationScope : IDisposable
    {
        private Action _onDispose;

        public ExternalAnimationScope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_onDispose == null)
            {
                return;
            }

            _onDispose.Invoke();
            _onDispose = null;
        }
    }
}
