using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using Modules.SignalR.Config;
using Modules.TurnSystem.Data;
using Modules.TurnSystem.Interfaces;
using R3;
using UnityEngine;

namespace Modules.TurnSystem.Services
{
    internal class SignalRTurnClient : ITurnEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly Subject<string> _turnAdvanced = new();
        private CancellationTokenSource _trackingCts;
        private IDisposable _subscription;

        public SignalRTurnClient(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public Observable<string> TurnAdvanced => _turnAdvanced;

        public async UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                return;
            }

            await StopTracking();

            _trackingCts = new CancellationTokenSource();
            try
            {
                var options = new TurnTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    session.GameId,
                    session.PlayerId);

                _subscription = await SubscribeAsync(options, _trackingCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TurnSystem] Failed to start tracking turns: {ex.Message}");
            }
        }

        public async UniTask DisconnectAsync()
        {
            await StopTracking();
        }

        public void Dispose()
        {
            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _subscription?.Dispose();
        }

        private async UniTask<IDisposable> SubscribeAsync(
            TurnTrackingOptions options,
            CancellationToken cancellationToken = default)
        {
            var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            connection.On<string>("TurnAdvanced", playerId => _turnAdvanced.OnNext(playerId));

            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = options.GameId,
                PlayerId = options.PlayerId
            }, cancellationToken);

            return new Subscription(connection);
        }

        private UniTask StopTracking()
        {
            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _trackingCts = null;

            _subscription?.Dispose();
            _subscription = null;

            return UniTask.CompletedTask;
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

