using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Interfaces;
using Modules.SignalR;
using Modules.TurnSystem.Interfaces;
using R3;
using UnityEngine;

namespace Modules.TurnSystem.Services
{
    internal class SignalRTurnClient : ITurnEventSource, ILobbyConnectionHandler, IDisposable
    {
        private readonly ISharedGameHubConnection _sharedHubConnection;
        private readonly Subject<string> _turnAdvanced = new();
        private readonly List<IDisposable> _handlerSubscriptions = new();

        public SignalRTurnClient(ISharedGameHubConnection sharedHubConnection)
        {
            _sharedHubConnection = sharedHubConnection ?? throw new ArgumentNullException(nameof(sharedHubConnection));
        }

        public Observable<string> TurnAdvanced => _turnAdvanced;

        public UniTask ConnectAsync(LobbySession session, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(session.GameId) || string.IsNullOrWhiteSpace(session.PlayerId))
            {
                return UniTask.CompletedTask;
            }

            ClearHandlers();
            RegisterHandlers();
            return UniTask.CompletedTask;
        }

        public UniTask DisconnectAsync()
        {
            ClearHandlers();
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            ClearHandlers();
        }

        private void RegisterHandlers()
        {
            ClearHandlers();
            _handlerSubscriptions.Add(_sharedHubConnection.Subscribe<string>("TurnAdvanced", playerId =>
                _turnAdvanced.OnNext(playerId)));
        }

        private void ClearHandlers()
        {
            foreach (var subscription in _handlerSubscriptions)
            {
                subscription?.Dispose();
            }

            _handlerSubscriptions.Clear();
        }
    }
}
