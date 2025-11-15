using Modules.PlayerHand.Data;
using R3;

namespace Modules.PlayerHand.Interfaces
{
    public interface IPlayerHandService
    {
        ReadOnlyReactiveProperty<PlayerHandState> ObserveHand(string playerId);
    }
}
