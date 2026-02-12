using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.BonusSystem.Data;
using Modules.BonusSystem.Interfaces;
using R3;
using UnityEngine;

namespace Modules.BonusSystem.Services
{
    public class BonusActionService
    {
        private IBonusActionClient _client;
        private readonly ReactiveProperty<bool> _isExecuting = new(false);

        public ReadOnlyReactiveProperty<bool> IsExecuting { get; private set; }

        [Zenject.Inject]
        private void Construct(IBonusActionClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            IsExecuting ??= _isExecuting.ToReadOnlyReactiveProperty();
        }

        public void Reset()
        {
            _isExecuting.Value = false;
        }

        public async UniTask<bool> UseBonusAsync(BonusUseRequest request, CancellationToken cancellationToken = default)
        {
            if (_isExecuting.Value)
            {
                Debug.LogWarning("[BonusSystem] Bonus use already in progress.");
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
                await _client.UseBonusAsync(request.BonusType, request.TargetPlayerId ?? string.Empty, cancellationToken);
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
