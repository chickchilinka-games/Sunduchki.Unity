using System;

namespace Modules.SignalR
{
    public interface ISignalRConnectionFactory
    {
        ISignalRConnection Create(Uri hubUri, string accessToken);
    }
}
