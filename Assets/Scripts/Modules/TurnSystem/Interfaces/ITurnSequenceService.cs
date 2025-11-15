using Modules.TurnSystem.Data;
using R3;

namespace Modules.TurnSystem.Interfaces
{
    public interface ITurnSequenceService
    {
        ReadOnlyReactiveProperty<TurnState> State { get; }

        Observable<string> CurrentPlayerStream { get; }

        void SetLocalPlayer(string playerId);

        void Reset();

        void AdvanceTo(string playerId);
    }
}
