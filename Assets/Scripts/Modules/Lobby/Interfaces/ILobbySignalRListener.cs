using Modules.Lobby.Data;

namespace Modules.Lobby.Interfaces
{
    public interface ILobbySignalRListener
    {
        void OnPlayerJoined(string playerId);

        void OnPlayerLeft(string playerId);

        void OnGameStarted();

        void OnGameEnded(GameEndedResultDto payload);

        void OnSetCompleted(string playerId, string rank);

        void OnConnectionClosed(string error);
    }
}
