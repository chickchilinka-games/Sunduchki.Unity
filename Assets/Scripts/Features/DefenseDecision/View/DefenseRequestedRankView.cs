using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;
using Modules.DefenseDecisionSystem.Data;
using Modules.DefenseDecisionSystem.Services;
using Modules.Lobby.Services;
using Modules.Players.Services;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Features.DefenseDecision.View
{
    public sealed class DefenseRequestedRankView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _askerLabel;
        [SerializeField] private float _hideDelaySeconds = 0.8f;

        private DefenseDecisionService _defenseService;
        private ICardRequestService _cardRequestService;
        private LobbyService _lobbyService;
        private PlayerRosterService _rosterService;
        private IDisposable _subscription;
        private IDisposable _requestSubscription;
        private bool _isDefenseActive;
        private bool _isRequestActive;
        private string _requestedRank = string.Empty;
        private string _askerId = string.Empty;
        private CancellationTokenSource _hideCts;
        private readonly HashSet<string> _requestedPlayerIds = new(StringComparer.Ordinal);

        [Inject]
        public void Construct(
            DefenseDecisionService defenseService,
            ICardRequestService cardRequestService,
            LobbyService lobbyService,
            PlayerRosterService rosterService)
        {
            _defenseService = defenseService ?? throw new ArgumentNullException(nameof(defenseService));
            _cardRequestService = cardRequestService ?? throw new ArgumentNullException(nameof(cardRequestService));
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _rosterService = rosterService ?? throw new ArgumentNullException(nameof(rosterService));
        }

        private void OnEnable()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _subscription = _defenseService.Prompt.Subscribe(OnPromptChanged);
            _requestSubscription = _cardRequestService.State.Subscribe(OnRequestChanged);
            OnPromptChanged(_defenseService.CurrentPrompt);
            OnRequestChanged(_cardRequestService.Current);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
            _requestSubscription?.Dispose();
            _requestSubscription = null;
            _hideCts?.Cancel();
            _hideCts?.Dispose();
            _hideCts = null;
            Hide();
        }

        private void OnPromptChanged(DefenseDecisionPrompt prompt)
        {
            if (prompt == null)
            {
                _isDefenseActive = false;
                _askerId = string.Empty;
                UpdateDisplay();
                return;
            }

            var localId = _lobbyService.StateContext?.Data.PlayerId;
            if (string.IsNullOrWhiteSpace(localId) ||
                !string.Equals(prompt.TargetId, localId, StringComparison.Ordinal))
            {
                _isDefenseActive = false;
                _askerId = string.Empty;
                UpdateDisplay();
                return;
            }

            _isDefenseActive = true;
            _requestedRank = prompt.Rank ?? string.Empty;
            _askerId = prompt.AskerId ?? string.Empty;
            UpdateDisplay();
        }

        private void OnRequestChanged(CardRequestState state)
        {
            if (!state.HasEvent)
            {
                return;
            }

            var evt = state.LastEvent;
            var localId = _lobbyService.StateContext?.Data.PlayerId;
            if (string.IsNullOrWhiteSpace(localId) ||
                !string.Equals(evt.TargetId, localId, StringComparison.Ordinal))
            {
                return;
            }

            switch (evt.EventType)
            {
                case CardRequestEventType.Requested:
                    _isRequestActive = true;
                    _requestedRank = evt.Rank ?? string.Empty;
                    _askerId = evt.AskerId ?? string.Empty;
                    UpdateDisplay();
                    break;
                case CardRequestEventType.Transferred:
                case CardRequestEventType.Denied:
                    if (!_isRequestActive)
                    {
                        ShowFallbackRequest(evt);
                    }
                    _isRequestActive = false;
                    _requestedRank = string.Empty;
                    _askerId = string.Empty;
                    UpdateDisplay();
                    break;
            }
        }

        private void UpdateDisplay()
        {
            if (_isDefenseActive || _isRequestActive)
            {
                CancelHide();
                Show(_requestedRank);
            }
            else
            {
                ScheduleHide();
            }
        }

        private void Show(string rank)
        {
            SetVisible(true);

            if (_askerLabel != null)
            {
                var askerName = ResolvePlayerName(_askerId);
                _askerLabel.text = string.IsNullOrWhiteSpace(askerName)
                    ? $"Opponent asked for {FormatRank(rank)}"
                    : $"{askerName} asked for {FormatRank(rank)}";
            }
        }

        private void Hide()
        {
            SetVisible(false);

            if (_askerLabel != null)
            {
                _askerLabel.text = string.Empty;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_root == null)
            {
                return;
            }

            if (ReferenceEquals(_root, gameObject))
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
                return;
            }

            _root.SetActive(visible);
        }

        private void ScheduleHide()
        {
            CancelHide();
            _hideCts = new CancellationTokenSource();
            HideDelayedAsync(_hideCts.Token).Forget();
        }

        private async UniTaskVoid HideDelayedAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, _hideDelaySeconds)), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!token.IsCancellationRequested)
            {
                Hide();
            }
        }

        private void CancelHide()
        {
            _hideCts?.Cancel();
            _hideCts?.Dispose();
            _hideCts = null;
        }

        private void ShowFallbackRequest(CardRequestEvent evt)
        {
            _isRequestActive = true;
            _requestedRank = "???";
            _askerId = evt.AskerId ?? string.Empty;
            UpdateDisplay();
            _isRequestActive = false;
            ScheduleHide();
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

        private static string FormatRank(string rank)
        {
            var normalized = string.IsNullOrWhiteSpace(rank) ? string.Empty : rank.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            var word = normalized switch
            {
                "two" => "two",
                "three" => "three",
                "four" => "four",
                "five" => "five",
                "six" => "six",
                "seven" => "seven",
                "eight" => "eight",
                "nine" => "nine",
                "ten" => "ten",
                "jack" => "jack",
                "queen" => "queen",
                "king" => "king",
                "ace" => "ace",
                _ => normalized
            };

            return char.ToUpperInvariant(word[0]) + word.Substring(1);
        }
    }
}
