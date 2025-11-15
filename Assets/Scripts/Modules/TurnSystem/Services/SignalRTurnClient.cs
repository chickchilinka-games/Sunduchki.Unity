using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.SignalR;
using Modules.TurnSystem.Data;
using Modules.TurnSystem.Interfaces;

namespace Modules.TurnSystem.Services
{
    public class SignalRTurnClient : ITurnSignalClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;

        public SignalRTurnClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async UniTask<IDisposable> SubscribeAsync(
            TurnTrackingOptions options,
            Action<string> onTurnAdvanced,
            CancellationToken cancellationToken = default)
        {
            if (onTurnAdvanced == null)
            {
                throw new ArgumentNullException(nameof(onTurnAdvanced));
            }

            var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            connection.On<string>("TurnAdvanced", onTurnAdvanced);

            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = options.GameId,
                PlayerId = options.PlayerId
            }, cancellationToken);

            return new Subscription(connection);
        }

        private sealed class Subscription : IDisposable
        {
            private readonly ISignalRConnection _connection;
            private bool _disposed;

            public Subscription(ISignalRConnection connection)
            {
                _connection = connection;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _connection.RemoveHandler("TurnAdvanced");
                _connection.StopAsync().Forget();
                _connection.Dispose();
            }
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }
    }
}
