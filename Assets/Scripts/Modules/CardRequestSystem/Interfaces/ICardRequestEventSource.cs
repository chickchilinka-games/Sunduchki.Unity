using Modules.CardRequestSystem.Data;
using R3;

namespace Modules.CardRequestSystem.Interfaces
{
    public interface ICardRequestEventSource
    {
        Observable<CardRequestedEvent> Requested { get; }
        Observable<CardRequestTransferredEvent> Transferred { get; }
        Observable<CardRequestDeniedEvent> Denied { get; }
    }
}

