using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;

namespace Modules.Lobby.Interfaces
{
    public interface ILobbyApiClient
    {
        UniTask<CreateGameResult> CreateGameAsync(CreateGameOptions options, CancellationToken cancellationToken = default);

        UniTask<JoinGameResult> JoinGameAsync(string gameId, JoinGameOptions options, CancellationToken cancellationToken = default);

        UniTask StartGameAsync(string gameId, CancellationToken cancellationToken = default);
    }
}
