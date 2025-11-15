using Modules.Lobby.Data;

namespace Modules.Lobby.Interfaces
{
    public interface ILobbySignalRListener
    {
        void OnPlayerJoined(string playerId, string playerName);

        void OnPlayerLeft(string playerId);

        void OnGameStarted();

        void OnGameEnded(object payload);
    }
}
