using Modules.Lobby.Data;

namespace Modules.Lobby.Config
{
    public interface ILobbyApiConfigProvider
    {
        LobbyApiConfig GetConfig();
    }
}
