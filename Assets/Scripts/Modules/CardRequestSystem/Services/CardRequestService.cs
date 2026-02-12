using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.CardRequestSystem.Model;
using R3;
using UnityEngine;

namespace Modules.CardRequestSystem.Services
{
    public class CardRequestService
    {
        private CardRequestModel _model;
        private ICardRequestCommandClient _commandClient;

        [Zenject.Inject]
        private void Construct(
            CardRequestModel model,
            ICardRequestCommandClient commandClient)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _commandClient = commandClient ?? throw new ArgumentNullException(nameof(commandClient));
        }

        public ReadOnlyReactiveProperty<CardRequestState> State => _model.State;

        public CardRequestState Current => _model.Current;

        public async UniTask<bool> AskAsync(string rank, string targetPlayerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rank) || string.IsNullOrWhiteSpace(targetPlayerId))
            {
                Debug.LogWarning("[CardRequestSystem] Invalid ask request.");
                return false;
            }

            try
            {
                await _commandClient.AskAsync(rank, targetPlayerId, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardRequestSystem] Ask request failed: {ex.Message}");
                return false;
            }
        }
    }
}
