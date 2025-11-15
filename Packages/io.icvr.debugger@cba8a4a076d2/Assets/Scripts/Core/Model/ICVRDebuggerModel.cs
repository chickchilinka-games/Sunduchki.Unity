using R3;

namespace Core.Model
{
    internal class ICVRDebuggerModel
    {
        public bool IsInitialized { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsVisible => _isVisible;

        private readonly ReactiveProperty<bool> _isVisible = new();
        
        public void Initialize()
        {
            IsInitialized = true;
        }
        internal void SetVisible(bool isVisible)
        {
            _isVisible.Value = isVisible;
        }
    }
}