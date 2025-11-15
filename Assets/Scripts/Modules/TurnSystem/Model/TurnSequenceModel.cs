using Modules.TurnSystem.Data;
using R3;

namespace Modules.TurnSystem.Model
{
    public class TurnSequenceModel
    {
        private readonly ReactiveProperty<TurnState> _state = new(TurnState.Default);

        public ReadOnlyReactiveProperty<TurnState> State => _state;

        public TurnState Current => _state.Value;

        public void SetState(TurnState state)
        {
            _state.Value = state;
        }
    }
}
