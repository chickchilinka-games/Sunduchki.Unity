using Modules.PlayerHand.Data;
using R3;

namespace Modules.PlayerHand.Interfaces
{
    public interface IPlayerHandEventSource
    {
        Observable<PlayerHandSnapshotEvent> StandardSnapshot { get; }
        Observable<PlayerHandStandardCardEvent> StandardCardAdded { get; }
        Observable<PlayerHandStandardCardEvent> StandardCardRemoved { get; }
        Observable<PlayerHandBonusCardEvent> BonusCardAdded { get; }
        Observable<PlayerHandBonusCardEvent> BonusCardRemoved { get; }
        Observable<CardsReceivedEvent> CardsReceived { get; }
    }
}

