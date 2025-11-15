using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;

namespace Modules.Lobby.Providers
{
    public class SignalRLobbyClient : ILobbySignalRClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private ISignalRConnection _connection;
        private ILobbySignalRListener _listener;

        public SignalRLobbyClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async UniTask ConnectAsync(
            LobbySignalRConnectionOptions options,
            ILobbySignalRListener listener,
            CancellationToken cancellationToken = default)
        {
            if (options.HubUri == null)
            {
                throw new ArgumentException("Hub URI must be provided.", nameof(options));
            }

            await DisconnectAsync(cancellationToken);

            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            RegisterHandlers(_connection, _listener);

            await _connection.StartAsync(cancellationToken);
        }

        public async UniTask JoinGameAsync(LobbySignalRJoinPayload payload, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            var request = new JoinGameRequestDto
            {
                GameId = payload.GameId,
                PlayerId = payload.PlayerId
            };

            await _connection.InvokeAsync("JoinGame", request, cancellationToken);
        }

        public async UniTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                return;
            }

            try
            {
                await _connection.StopAsync(cancellationToken);
            }
            finally
            {
                _connection.Dispose();
                _connection = null;
                _listener = null;
            }
        }

        private void RegisterHandlers(ISignalRConnection connection, ILobbySignalRListener listener)
        {
            connection.On<string, string>("PlayerJoined", (playerId, name) =>
            {
                listener.OnPlayerJoined(playerId, name);
            });

            connection.On<string>("PlayerLeft", listener.OnPlayerLeft);
            connection.On("GameStarted", listener.OnGameStarted);

            connection.On<Dictionary<string, int>>("GameEnded", payload =>
            {
                listener.OnGameEnded(payload ?? new Dictionary<string, int>());
            });
        }

        private void EnsureConnected()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("SignalR connection has not been established. Call ConnectAsync first.");
            }
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }
    }
}
