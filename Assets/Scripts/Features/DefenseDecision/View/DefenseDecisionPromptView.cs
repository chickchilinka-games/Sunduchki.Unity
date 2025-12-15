using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Services;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.DefenseDecision.View
{
    public sealed class DefenseDecisionPromptView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _messageLabel;
        [SerializeField] private Button _giveCardsButton;
        [SerializeField] private Transform _optionsRoot;
        [SerializeField] private DefenseDecisionOptionView _optionViewPrefab;

        private readonly List<DefenseDecisionOptionView> _options = new();
        private DefenseDecisionService _defenseService;
        private LobbyService _lobbyService;
        private IDisposable _promptSubscription;
        private bool _isSubmitting;

        [Inject]
        public void Construct(DefenseDecisionService defenseService, LobbyService lobbyService)
        {
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        private void OnEnable()
        {
            if (_giveCardsButton != null)
            {
                _giveCardsButton.onClick.AddListener(OnGiveCardsClicked);
            }

            _promptSubscription = _defenseService.Prompt.Subscribe(OnPromptChanged);
            OnPromptChanged(_defenseService.CurrentPrompt);
        }

        private void OnDisable()
        {
            if (_giveCardsButton != null)
            {
                _giveCardsButton.onClick.RemoveListener(OnGiveCardsClicked);
            }

            _promptSubscription?.Dispose();
            _promptSubscription = null;
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
            BuildOptions(prompt);
            UpdateButtonsState(!_isSubmitting);
        }

        private void HidePrompt()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            UpdateMessage(null);
            ClearOptions();
            _isSubmitting = false;
            UpdateButtonsState(false);
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
            var players = _lobbyService.Players?.CurrentValue;
            if (players == null)
            {
                return string.Empty;
            }

            foreach (var player in players)
            {
                if (player.Id == playerId)
                {
                    return player.Name;
                }
            }

            return string.Empty;
        }

        private void BuildOptions(DefenseDecisionPrompt prompt)
        {
            ClearOptions();
            if (_optionsRoot == null || _optionViewPrefab == null)
            {
                return;
            }

            foreach (var option in prompt.DefenseOptions)
            {
                var label = string.IsNullOrWhiteSpace(option) ? "Use bonus" : $"Use {option}";
                var view = Instantiate(_optionViewPrefab, _optionsRoot);
                view.Initialize(label, () => OnBonusOptionClicked(option));
                _options.Add(view);
            }
        }

        private void ClearOptions()
        {
            foreach (var view in _options)
            {
                if (view != null)
                {
                    view.ResetView();
                    Destroy(view.gameObject);
                }
            }
            _options.Clear();
        }

        private void UpdateButtonsState(bool interactable)
        {
            if (_giveCardsButton != null)
            {
                _giveCardsButton.interactable = interactable;
            }

            foreach (var option in _options)
            {
                option?.SetInteractable(interactable);
            }
        }

        private void OnGiveCardsClicked()
        {
            SubmitDecision(false, null).Forget();
        }

        private void OnBonusOptionClicked(string bonusType)
        {
            SubmitDecision(true, bonusType).Forget();
        }

        private async UniTaskVoid SubmitDecision(bool useBonus, string bonusType)
        {
            if (_isSubmitting)
            {
                return;
            }

            _isSubmitting = true;
            UpdateButtonsState(false);

            try
            {
                await _defenseService.SubmitDecisionAsync(new DefenseDecisionSubmitRequest(useBonus, bonusType));
            }
            finally
            {
                _isSubmitting = false;
                UpdateButtonsState(true);
            }
        }
    }
}
