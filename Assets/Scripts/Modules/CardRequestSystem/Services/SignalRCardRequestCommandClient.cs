using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR.Config;
using Modules.SignalR;
using UnityEngine;

namespace Modules.CardRequestSystem.Services
{
    internal class SignalRCardRequestCommandClient : ICardRequestCommandClient, ILobbyConnectionHandler
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private ISignalRConnection _connection;
        private string _gameId = string.Empty;
        private string _playerId = string.Empty;

        public SignalRCardRequestCommandClient(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public async UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                return;
            }

            var options = new CardRequestCommandOptions(
                _configProvider.GetHubUri(),
                _configProvider.GetAccessToken(),
                session.GameId,
                session.PlayerId);

            await ConnectAsync(options, cancellationToken);
        }

        private async UniTask ConnectAsync(CardRequestCommandOptions options, CancellationToken cancellationToken = default)
        {
            await DisconnectAsync();

            var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = options.GameId,
                PlayerId = options.PlayerId
            }, cancellationToken);

            _connection = connection;
            _gameId = options.GameId ?? string.Empty;
            _playerId = options.PlayerId ?? string.Empty;
        }

        public async UniTask DisconnectAsync()
        {
            var connection = Interlocked.Exchange(ref _connection, null);
            if (connection == null)
            {
                return;
            }

            try
            {
                await connection.StopAsync();
            }
            finally
            {
                connection.Dispose();
            }

            _gameId = string.Empty;
            _playerId = string.Empty;
        }

        public async UniTask AskAsync(string rank, string targetPlayerId, CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                Debug.LogWarning("[CardRequestSystem] Ask called without an active SignalR connection.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_gameId) || string.IsNullOrWhiteSpace(_playerId))
            {
                Debug.LogWarning("[CardRequestSystem] Ask called without game/player session.");
                return;
            }

            if (string.IsNullOrWhiteSpace(rank) || string.IsNullOrWhiteSpace(targetPlayerId))
            {
                Debug.LogWarning("[CardRequestSystem] Invalid ask request.");
                return;
            }

            await _connection.InvokeAsync("Ask", new AskRequestDto
            {
                GameId = _gameId,
                AskerId = _playerId,
                TargetId = targetPlayerId,
                Rank = rank
            }, cancellationToken);
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }

        private sealed class AskRequestDto
        {
            public string GameId { get; set; }
            public string AskerId { get; set; }
            public string TargetId { get; set; }
            public string Rank { get; set; }
        }
    }
}
