using Modules.CardRequestSystem.Data;
using R3;

namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestService
    {
        ReadOnlyReactiveProperty<CardRequestState> State { get; }

        CardRequestState Current { get; }
    }
}
