using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;
using Modules.SignalR;
using UnityEngine;

namespace Modules.DefenseDecisionSystem.Services
{
    public class SignalRDefenseDecisionClient : IDefenseDecisionClient
    {
        private readonly ISignalRConnectionFactory _connectionFactory;
        private ISignalRConnection _connection;

        public SignalRDefenseDecisionClient(ISignalRConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async UniTask ConnectAsync(DefenseDecisionConnectionOptions options, CancellationToken cancellationToken = default)
        {
            await DisconnectAsync();

            try
            {
                var connection = _connectionFactory.Create(options.HubUri, options.AccessToken);
                await connection.StartAsync(cancellationToken);
                await connection.InvokeAsync("JoinGame", new JoinGameRequestDto
                {
                    GameId = options.GameId,
                    PlayerId = options.PlayerId
                }, cancellationToken);

                _connection = connection;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DefenseDecision] Failed to connect: {ex.Message}");
                await DisconnectAsync();
                throw;
            }
        }

        public async UniTask SubmitDecisionAsync(DefenseDecisionSubmitPayload payload, CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                Debug.LogWarning("[DefenseDecision] SubmitDecisionAsync called without an active connection.");
                return;
            }

            await _connection.InvokeAsync("SubmitDefenseDecision", new DefenseDecisionRequestDto
            {
                GameId = payload.GameId,
                TargetId = payload.TargetPlayerId,
                UseBonus = payload.UseBonus,
                BonusType = string.IsNullOrWhiteSpace(payload.BonusType) ? null : payload.BonusType
            }, cancellationToken);
        }

        public async UniTask DisconnectAsync()
        {
            if (_connection == null)
            {
                return;
            }

            try
            {
                await _connection.StopAsync();
            }
            finally
            {
                _connection.Dispose();
                _connection = null;
            }
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
    }
}
