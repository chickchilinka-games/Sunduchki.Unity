using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using UnityEngine;

namespace Modules.CardRequestSystem.Services
{
    public class CardRequestCommandService
    {
        private readonly ICardRequestCommandClient _client;
        private string _gameId = string.Empty;
        private string _playerId = string.Empty;

        public CardRequestCommandService(ICardRequestCommandClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public void Configure(string gameId, string playerId)
        {
            _gameId = gameId ?? string.Empty;
            _playerId = playerId ?? string.Empty;
        }

        public void Reset()
        {
            _gameId = string.Empty;
            _playerId = string.Empty;
        }

        public async UniTask ConnectAsync(CardRequestCommandOptions options, CancellationToken cancellationToken = default)
        {
            _gameId = options.GameId;
            _playerId = options.PlayerId;
            await _client.ConnectAsync(options, cancellationToken);
        }

        public async UniTask DisconnectAsync()
        {
            await _client.DisconnectAsync();
            Reset();
        }

        public async UniTask<bool> AskAsync(string rank, string targetPlayerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_gameId) || string.IsNullOrWhiteSpace(_playerId))
            {
                Debug.LogWarning("[CardRequestSystem] Command service is not configured with game/player id.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(rank) || string.IsNullOrWhiteSpace(targetPlayerId))
            {
                Debug.LogWarning("[CardRequestSystem] Invalid ask request.");
                return false;
            }

            try
            {
                var payload = new CardRequestAskPayload(_gameId, _playerId, targetPlayerId, rank);
                await _client.AskAsync(payload, cancellationToken);
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
