using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Interfaces;
using R3;
using UnityEngine;

namespace Modules.DefenseDecisionSystem.Services
{
    public class DefenseDecisionService : IDefenseDecisionPromptWriter
    {
        private readonly IDefenseDecisionClient _client;
        private readonly ReactiveProperty<DefenseDecisionPrompt> _prompt;
        private string _gameId = string.Empty;
        private string _playerId = string.Empty;

        public DefenseDecisionService(IDefenseDecisionClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _prompt = new ReactiveProperty<DefenseDecisionPrompt>(null);
            Prompt = _prompt.ToReadOnlyReactiveProperty();
        }

        public ReadOnlyReactiveProperty<DefenseDecisionPrompt> Prompt { get; }
        public DefenseDecisionPrompt CurrentPrompt => _prompt.Value;

        public void Configure(string gameId, string playerId)
        {
            _gameId = gameId ?? string.Empty;
            _playerId = playerId ?? string.Empty;
        }

        public void Reset()
        {
            _gameId = string.Empty;
            _playerId = string.Empty;
            ClearPrompt();
        }

        public async UniTask<bool> SubmitDecisionAsync(DefenseDecisionSubmitRequest request, CancellationToken cancellationToken = default)
        {
            var prompt = _prompt.Value;
            if (prompt == null)
            {
                Debug.LogWarning("[DefenseDecision] Tried to submit decision without an active prompt.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_gameId) || string.IsNullOrWhiteSpace(_playerId))
            {
                Debug.LogWarning("[DefenseDecision] Service is not configured with game/player identifiers.");
                return false;
            }

            if (request.UseBonus && string.IsNullOrWhiteSpace(request.BonusType))
            {
                Debug.LogWarning("[DefenseDecision] Bonus type is required when using a defense bonus.");
                return false;
            }

            var payload = new DefenseDecisionSubmitPayload(
                _gameId,
                prompt.TargetId,
                request.UseBonus,
                NormalizeBonusType(request.BonusType));

            try
            {
                await _client.SubmitDecisionAsync(payload, cancellationToken);
                ClearPrompt();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DefenseDecision] Failed to submit defense decision: {ex.Message}");
                return false;
            }
        }

        void IDefenseDecisionPromptWriter.PublishPrompt(DefenseDecisionPrompt prompt)
        {
            _prompt.Value = prompt;
        }

        void IDefenseDecisionPromptWriter.ClearPrompt()
        {
            ClearPrompt();
        }

        private void ClearPrompt()
        {
            _prompt.Value = null;
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
