using Modules.BonusSystem.Data;
using R3;

namespace Modules.BonusSystem.Interfaces
{
    public interface IBonusUsageEventSource
    {
        Observable<BonusUsedEvent> BonusUsed { get; }
    }
}
