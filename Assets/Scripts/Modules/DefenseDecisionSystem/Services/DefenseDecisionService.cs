using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;
using R3;
using UnityEngine;

namespace Modules.DefenseDecisionSystem.Services
{
    public class DefenseDecisionService
    {
        private IDefenseDecisionClient _client;
        private readonly ReactiveProperty<DefenseDecisionPrompt> _prompt = new(null);

        public ReadOnlyReactiveProperty<DefenseDecisionPrompt> Prompt { get; private set; }

        [Zenject.Inject]
        private void Construct(IDefenseDecisionClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            Prompt ??= _prompt.ToReadOnlyReactiveProperty();
        }

        public DefenseDecisionPrompt CurrentPrompt => _prompt.Value;

        public void Reset()
        {
            ClearPrompt();
        }

        public void PublishPrompt(DefenseDecisionPrompt prompt)
        {
            _prompt.Value = prompt;
        }

        public void ClearPrompt()
        {
            _prompt.Value = null;
        }

        public async UniTask<bool> SubmitDecisionAsync(DefenseDecisionSubmitRequest request, CancellationToken cancellationToken = default)
        {
            var prompt = _prompt.Value;
            if (prompt == null)
            {
                Debug.LogWarning("[DefenseDecision] Tried to submit decision without an active prompt.");
                return false;
            }

            if (request.UseBonus && string.IsNullOrWhiteSpace(request.BonusType))
            {
                Debug.LogWarning("[DefenseDecision] Bonus type is required when using a defense bonus.");
                return false;
            }

            try
            {
                await _client.SubmitDecisionAsync(
                    prompt.TargetId,
                    request.UseBonus,
                    NormalizeBonusType(request.BonusType),
                    cancellationToken);
                ClearPrompt();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DefenseDecision] Failed to submit defense decision: {ex.Message}");
                return false;
            }
        }

        private static string NormalizeBonusType(string bonusType)
        {
            if (string.IsNullOrWhiteSpace(bonusType))
            {
                return string.Empty;
            }

            return bonusType.Trim();
        }
    }
}
