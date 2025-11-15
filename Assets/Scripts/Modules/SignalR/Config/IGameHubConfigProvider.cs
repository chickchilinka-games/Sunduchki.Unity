using System;

namespace Modules.SignalR.Config
{
    public interface IGameHubConfigProvider
    {
        Uri GetHubUri();
        string GetAccessToken();
    }
}
