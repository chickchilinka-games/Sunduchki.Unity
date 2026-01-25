using Modules.DeckSystem.Data;
using R3;

namespace Modules.DeckSystem.Interfaces
{
    public interface IDeckDrawEventStream
    {
        Observable<DeckCardDrawnEvent> StandardDrawn { get; }
        Observable<DeckBonusCardDrawnEvent> BonusDrawn { get; }
    }
}
