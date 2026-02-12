using Modules.DeckSystem.Data;
using R3;

namespace Modules.DeckSystem.Interfaces
{
    public interface IDeckEventSource
    {
        Observable<DeckConfiguredEvent> Configured { get; }
        Observable<DeckCardDrawnEvent> StandardDrawn { get; }
        Observable<DeckBonusCardDrawnEvent> BonusDrawn { get; }
        Observable<DeckPeekedEvent> Peeked { get; }
        Observable<DeckAdjustedEvent> Adjusted { get; }
    }
}

