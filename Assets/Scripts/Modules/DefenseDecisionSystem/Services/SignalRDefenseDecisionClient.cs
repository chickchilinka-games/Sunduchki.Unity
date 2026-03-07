using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using R3;
using UnityEngine;

namespace Modules.DefenseDecisionSystem.Services
{
    internal class SignalRDefenseDecisionClient : IDefenseDecisionClient, IDefenseDecisionEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private readonly Subject<DefenseDecisionRequestedEvent> _requested = new();
        private readonly List<IDisposable> _handlerSubscriptions = new();

        private bool _connected;
        private string _gameId = string.Empty;

        public SignalRDefenseDecisionClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public Observable<DefenseDecisionRequestedEvent> Requested => _requested;

        public UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                Debug.LogWarning("[DefenseDecision] Cannot track defense decisions without lobby identifiers.");
                return UniTask.CompletedTask;
            }

            ClearHandlers();
            _gameId = session.GameId ?? string.Empty;
            RegisterHandlers();
            _connected = true;
            return UniTask.CompletedTask;
        }

        public async UniTask SubmitDecisionAsync(string targetPlayerId, bool useBonus, string bonusType, CancellationToken cancellationToken = default)
        {
            if (!_connected)
            {
                Debug.LogWarning("[DefenseDecision] SubmitDecisionAsync called without an active connection.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_gameId))
            {
                Debug.LogWarning("[DefenseDecision] SubmitDecisionAsync called without game session.");
                return;
            }

            await _sharedHubConnection.InvokeAsync("SubmitDefenseDecision", new DefenseDecisionRequestDto
            {
                GameId = _gameId,
                TargetId = targetPlayerId,
                UseBonus = useBonus,
                BonusType = string.IsNullOrWhiteSpace(bonusType) ? null : bonusType
            }, cancellationToken);
        }

        public UniTask DisconnectAsync()
        {
            ClearHandlers();
            _connected = false;
            _gameId = string.Empty;
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            ClearHandlers();
            _connected = false;
            _gameId = string.Empty;
        }

        private void RegisterHandlers()
        {
            ClearHandlers();
            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<DefenseDecisionRequestedDto>("DefenseDecisionRequested", payload =>
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
            }));
        }

        private void ClearHandlers()
        {
            foreach (var subscription in _handlerSubscriptions)
            {
                subscription?.Dispose();
            }

            _handlerSubscriptions.Clear();
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
