using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;

namespace Modules.Lobby.Interfaces
{
    public interface ILobbyConnectionHandler
    {
        UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default);

        UniTask DisconnectAsync();
    }
}
