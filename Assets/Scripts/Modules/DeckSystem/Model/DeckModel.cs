using Modules.DeckSystem.Data;
using R3;

namespace Modules.DeckSystem.Model
{
    public class DeckModel
    {
        private readonly ReactiveProperty<DeckState> _state = new(DeckState.Default);

        public ReadOnlyReactiveProperty<DeckState> State => _state;

        public DeckState Current => _state.Value;

        public void SetState(DeckState state)
        {
            _state.Value = state;
        }
    }
}
