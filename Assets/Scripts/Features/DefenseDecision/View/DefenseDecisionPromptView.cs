using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Services;
using Modules.Players.Services;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.DefenseDecision.View
{
    public sealed class DefenseDecisionPromptView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _messageLabel;
        private DefenseDecisionService _defenseService;
        private PlayerRosterService _rosterService;
        private IDisposable _promptSubscription;
        private IDisposable _rosterSubscription;
        private readonly HashSet<string> _requestedPlayerIds = new(StringComparer.Ordinal);

        [Inject]
        public void Construct(DefenseDecisionService defenseService, PlayerRosterService rosterService)
        {
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _rosterService = rosterService ?? throw new ArgumentNullException(nameof(rosterService));
        }

        private void OnEnable()
        {
            _promptSubscription = _defenseService.Prompt.Subscribe(OnPromptChanged);
            _rosterSubscription = _rosterService.Players.Subscribe(_ => UpdateMessage(_defenseService.CurrentPrompt));
            OnPromptChanged(_defenseService.CurrentPrompt);
        }

        private void OnDisable()
        {
            _promptSubscription?.Dispose();
            _promptSubscription = null;
            _rosterSubscription?.Dispose();
            _rosterSubscription = null;
            HidePrompt();
        }

        private void OnPromptChanged(DefenseDecisionPrompt prompt)
        {
            if (prompt == null)
            {
                HidePrompt();
                return;
            }

            ShowPrompt(prompt);
        }

        private void ShowPrompt(DefenseDecisionPrompt prompt)
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            UpdateMessage(prompt);
        }

        private void HidePrompt()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            UpdateMessage(null);
        }

        private void UpdateMessage(DefenseDecisionPrompt prompt)
        {
            if (_messageLabel == null)
            {
                return;
            }

            if (prompt == null)
            {
                _messageLabel.text = string.Empty;
                return;
            }

            var askerName = ResolvePlayerName(prompt.AskerId);
            _messageLabel.text = string.IsNullOrWhiteSpace(askerName)
                ? $"Opponent requested {prompt.Rank}"
                : $"{askerName} requested {prompt.Rank}";
        }

        private string ResolvePlayerName(string playerId)
        {
            if (_rosterService == null || string.IsNullOrWhiteSpace(playerId))
            {
                return string.Empty;
            }

            if (_rosterService.TryGetPlayer(playerId, out var info) &&
                !string.IsNullOrWhiteSpace(info.Name))
            {
                return info.Name;
            }

            if (_requestedPlayerIds.Add(playerId))
            {
                _rosterService.FetchPlayerAsync(playerId).Forget();
            }

            return string.Empty;
        }

    }
}
