using R3;

namespace Modules.TurnSystem.Interfaces
{
    public interface ITurnEventSource
    {
        Observable<string> TurnAdvanced { get; }
    }
}

