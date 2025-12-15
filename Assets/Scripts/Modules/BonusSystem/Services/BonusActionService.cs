using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Data;
using Modules.BonusSystem.Interfaces;
using R3;
using UnityEngine;

namespace Modules.BonusSystem.Services
{
    public class BonusActionService : IBonusActionService
    {
        private readonly IBonusActionClient _client;
        private readonly ReactiveProperty<bool> _isExecuting = new(false);
        private string _gameId;
        private string _playerId;

        public BonusActionService(IBonusActionClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            IsExecuting = _isExecuting.ToReadOnlyReactiveProperty();
        }

        public ReadOnlyReactiveProperty<bool> IsExecuting { get; }

        public void Configure(string gameId, string playerId)
        {
            _gameId = gameId ?? string.Empty;
            _playerId = playerId ?? string.Empty;
        }

        public void Reset()
        {
            _gameId = string.Empty;
            _playerId = string.Empty;
            _isExecuting.Value = false;
        }

        public async UniTask<bool> UseBonusAsync(BonusUseRequest request, CancellationToken cancellationToken = default)
        {
            if (_isExecuting.Value)
            {
                Debug.LogWarning("[BonusSystem] Bonus use already in progress.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_gameId) || string.IsNullOrWhiteSpace(_playerId))
            {
                Debug.LogError("[BonusSystem] BonusActionService is not configured with game/player id.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.BonusType))
            {
                Debug.LogWarning("[BonusSystem] Bonus type is empty.");
                return false;
            }

            _isExecuting.Value = true;
            try
            {
                var payload = new BonusUsePayload(
                    _gameId,
                    _playerId,
                    request.BonusType,
                    request.TargetPlayerId ?? string.Empty);

                await _client.UseBonusAsync(payload, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BonusSystem] Bonus use failed: {ex.Message}");
                return false;
            }
            finally
            {
                _isExecuting.Value = false;
            }
        }
    }
}
