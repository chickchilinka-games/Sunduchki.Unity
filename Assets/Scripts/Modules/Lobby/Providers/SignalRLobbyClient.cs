using System;
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
        private bool _suppressClosed;
        private readonly RejoinCoordinator _rejoin = new();

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
            _suppressClosed = false;
            RegisterHandlers(_connection, _listener);

            await _connection.StartAsync(cancellationToken);
        }

        public async UniTask JoinGameAsync(LobbySignalRJoinPayload payload, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            _rejoin.Update(payload);
            var request = new JoinGameRequestDto
            {
                GameId = payload.GameId,
                PlayerId = payload.PlayerId
            };

            await _connection.InvokeAsync("JoinGame", request, cancellationToken);
        }

        public async UniTask LeaveGameAsync(LobbySignalRLeavePayload payload, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            var request = new LeaveGameRequestDto
            {
                GameId = payload.GameId,
                PlayerId = payload.PlayerId
            };

            await _connection.InvokeAsync("LeaveGame", request, cancellationToken);
        }

        public async UniTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                return;
            }

            try
            {
                _suppressClosed = true;
                await _connection.StopAsync(cancellationToken);
            }
            finally
            {
                _connection.Dispose();
                _connection = null;
                _listener = null;
                _suppressClosed = false;
            }
        }

        private void RegisterHandlers(ISignalRConnection connection, ILobbySignalRListener listener)
        {
            connection.OnClosed(ex =>
            {
                if (_suppressClosed)
                {
                    return;
                }

                listener.OnConnectionClosed(ex?.Message);
            });

            connection.OnReconnected(_ =>
            {
                _rejoin.HandleReconnected(
                    JoinGameAsync,
                    reason => listener.OnConnectionClosed(reason));
            });

            connection.On<PlayerJoinedDto>("PlayerJoined", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnPlayerJoined(payload.PlayerId);
            });

            connection.On<string>("PlayerLeft", listener.OnPlayerLeft);
            connection.On("GameStarted", listener.OnGameStarted);

            connection.On<GameEndedResultDto>("GameEnded", payload =>
            {
                listener.OnGameEnded(payload ?? new GameEndedResultDto(
                    Array.Empty<GameEndedResultDto.PlayerChestResult>(),
                    Array.Empty<string>()));
            });

            connection.On<SetCompletedDto>("SetCompleted", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                listener.OnSetCompleted(payload.PlayerId, payload.Rank);
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

        private sealed class PlayerJoinedDto
        {
            public string PlayerId { get; set; }
        }

        private sealed class SetCompletedDto
        {
            public string PlayerId { get; set; }
            public string Rank { get; set; }
        }

        private sealed class LeaveGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }

        private sealed class RejoinCoordinator
        {
            private LobbySignalRJoinPayload _payload;

            public void Update(LobbySignalRJoinPayload payload)
            {
                _payload = payload;
            }

            public void HandleReconnected(
                Func<LobbySignalRJoinPayload, CancellationToken, UniTask> rejoinAsync,
                Action<string> onFailure)
            {
                if (string.IsNullOrWhiteSpace(_payload.GameId) ||
                    string.IsNullOrWhiteSpace(_payload.PlayerId))
                {
                    return;
                }

                UniTask.Void(async () =>
                {
                    try
                    {
                        await rejoinAsync(_payload, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        onFailure?.Invoke($"Rejoin failed: {ex.Message}");
                    }
                });
            }
        }

    }
}
