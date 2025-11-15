using Modules.CardRequestSystem.Data;
using R3;

namespace Modules.CardRequestSystem.Model
{
    public class CardRequestModel
    {
        private readonly ReactiveProperty<CardRequestState> _state = new(CardRequestState.Empty);

        public ReadOnlyReactiveProperty<CardRequestState> State => _state;

        public CardRequestState Current => _state.Value;

        public void SetState(CardRequestState state)
        {
            _state.Value = state;
        }
    }
}
