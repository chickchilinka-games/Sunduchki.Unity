using Modules.PlayerHand.Data;
using R3;

namespace Modules.PlayerHand.Interfaces
{
    public interface IPlayerHandEventSource
    {
        Observable<PlayerHandSnapshotEvent> HandSnapshot { get; }
        Observable<PlayerHandDeltaEvent> HandDelta { get; }
    }
}

