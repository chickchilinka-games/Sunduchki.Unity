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
        private ISignalRConnection _connection;

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

            _trackingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _trackingCts.Token;
            try
            {
                var options = new TurnTrackingOptions(
                    _configProvider.GetHubUri(),
                    _configProvider.GetAccessToken(),
                    session.GameId,
                    session.PlayerId);

                await SubscribeAsync(options, token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TurnSystem] Failed to start tracking turns: {ex.Message}");
                await StopTracking();
                throw;
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
            _trackingCts = null;
            var connection = Interlocked.Exchange(ref _connection, null);
            connection?.Dispose();
        }

        private async UniTask SubscribeAsync(
            TurnTrackingOptions options,
            CancellationToken cancellationToken = default)
        {
            var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
            connection.On<string>("TurnAdvanced", playerId => _turnAdvanced.OnNext(playerId));
            try
            {
                await connection.StartAsync(cancellationToken);
                await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
                {
                    GameId = options.GameId,
                    PlayerId = options.PlayerId
                }, cancellationToken);
            }
            catch
            {
                connection.RemoveHandler("TurnAdvanced");
                try
                {
                    await connection.StopAsync(cancellationToken);
                }
                catch
                {
                    // ignore stop errors on failed connect path
                }

                connection.Dispose();
                throw;
            }

            _connection = connection;
        }

        private async UniTask StopTracking()
        {
            _trackingCts?.Cancel();
            _trackingCts?.Dispose();
            _trackingCts = null;

            var connection = Interlocked.Exchange(ref _connection, null);
            if (connection == null)
            {
                return;
            }

            connection.RemoveHandler("TurnAdvanced");
            try
            {
                await connection.StopAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TurnSystem] Stop tracking failed: {ex.Message}");
            }
            finally
            {
                connection.Dispose();
            }
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }
    }
}

