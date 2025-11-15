using Modules.DeckSystem.Data;
using R3;

namespace Modules.DeckSystem.Interfaces
{
    public interface IDeckService
    {
        ReadOnlyReactiveProperty<DeckState> State { get; }

        DeckState Current { get; }
    }
}
