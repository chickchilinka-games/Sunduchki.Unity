using Modules.DefenseDecisionSystem.Data;
using R3;

namespace Modules.DefenseDecisionSystem.Interfaces
{
    public interface IDefenseDecisionEventSource
    {
        Observable<DefenseDecisionRequestedEvent> Requested { get; }
    }
}

