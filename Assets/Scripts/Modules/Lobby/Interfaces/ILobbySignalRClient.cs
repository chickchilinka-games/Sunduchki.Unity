using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;

namespace Modules.Lobby.Interfaces
{
    public interface ILobbySignalRClient
    {
        UniTask ConnectAsync(LobbySignalRConnectionOptions options, ILobbySignalRListener listener, CancellationToken cancellationToken = default);

        UniTask JoinGameAsync(LobbySignalRJoinPayload payload, CancellationToken cancellationToken = default);

        UniTask LeaveGameAsync(LobbySignalRLeavePayload payload, CancellationToken cancellationToken = default);

        UniTask DisconnectAsync(CancellationToken cancellationToken = default);
    }
}
