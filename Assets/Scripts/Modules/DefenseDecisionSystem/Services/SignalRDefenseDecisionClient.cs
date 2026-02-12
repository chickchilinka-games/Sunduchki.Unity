using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using Modules.SignalR.Config;
using R3;
using UnityEngine;

namespace Modules.DefenseDecisionSystem.Services
{
    internal class SignalRDefenseDecisionClient : IDefenseDecisionClient, IDefenseDecisionEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private readonly IGameHubConfigProvider _configProvider;
        private readonly Subject<DefenseDecisionRequestedEvent> _requested = new();
        private ISignalRConnection _connection;
        private string _gameId = string.Empty;

        public SignalRDefenseDecisionClient(
            ISignalRConnectionFactory connectionFactory,
            IGameHubConfigProvider configProvider)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public Observable<DefenseDecisionRequestedEvent> Requested => _requested;

        public async UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[DefenseDecision] Cannot track defense decisions without lobby identifiers.");
                return;
            }

            await DisconnectCoreAsync();

            var connection = _connectionFactory.Create(_configProvider.GetHubUri(), _configProvider.GetAccessToken());
            RegisterHandlers(connection);
            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
            {
                GameId = session.GameId,
                PlayerId = session.PlayerId
            }, cancellationToken);

            _connection = connection;
            _gameId = session.GameId ?? string.Empty;
        }

        public async UniTask SubmitDecisionAsync(string targetPlayerId, bool useBonus, string bonusType, CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                Debug.LogWarning("[DefenseDecision] SubmitDecisionAsync called without an active connection.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_gameId))
            {
                Debug.LogWarning("[DefenseDecision] SubmitDecisionAsync called without game session.");
                return;
            }

            await _connection.InvokeAsync("SubmitDefenseDecision", new DefenseDecisionRequestDto
            {
                GameId = _gameId,
                TargetId = targetPlayerId,
                UseBonus = useBonus,
                BonusType = string.IsNullOrWhiteSpace(bonusType) ? null : bonusType
            }, cancellationToken);
        }

        public async UniTask DisconnectAsync()
        {
            await DisconnectCoreAsync();
        }

        public void Dispose()
        {
            DisconnectCoreAsync().Forget();
        }

        private void RegisterHandlers(ISignalRConnection connection)
        {
            connection.On<DefenseDecisionRequestedDto>("DefenseDecisionRequested", payload =>
            {
                if (payload == null)
                {
                    return;
                }

                _requested.OnNext(new DefenseDecisionRequestedEvent(
                    payload.AskerId,
                    payload.TargetPlayerId,
                    payload.Rank,
                    payload.DefenseOptions ?? new List<string>()));
            });
        }

        private async UniTask DisconnectCoreAsync()
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
        }

        private sealed class JoinGameRequestDto
        {
            public string GameId { get; set; }
            public string PlayerId { get; set; }
        }

        private sealed class DefenseDecisionRequestDto
        {
            public string GameId { get; set; }
            public string TargetId { get; set; }
            public bool UseBonus { get; set; }
            public string BonusType { get; set; }
        }

        private sealed class DefenseDecisionRequestedDto
        {
            public string AskerId { get; set; }
            public string TargetPlayerId { get; set; }
            public string Rank { get; set; }
            public List<string> DefenseOptions { get; set; }
        }
    }
}

