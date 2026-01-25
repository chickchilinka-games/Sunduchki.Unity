using System;
using Cysharp.Threading.Tasks;
using Modules.Players.Services;
using Modules.TurnSystem.Services;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Features.LobbyImpl.View
{
    /// <summary>
    /// Displays a lobby player's name and collected chest count.
    /// </summary>
    public class LobbyPlayerStatsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _chestCountLabel;
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private string _emptyName = "--";
        [SerializeField] private string _yourTurnText = "Your turn";
        [SerializeField] private string _opponentTurnText = "Opponent's turn";

        public string CurrentPlayerId { get; private set; }
        private PlayerRosterService _rosterService;
        private TurnSequenceService _turnService;
        private IDisposable _subscription;
        private IDisposable _turnSubscription;

        [Inject]
        public void Construct(PlayerRosterService rosterService, TurnSequenceService turnService)
        {
            _rosterService = rosterService ?? throw new ArgumentNullException(nameof(rosterService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
        }

        private void OnEnable()
        {
            if (_rosterService == null)
            {
                return;
            }

            _subscription = _rosterService.Players
                .Subscribe(_ => UpdateNameFromRoster());

            if (_turnService != null)
            {
                _turnSubscription = _turnService.State
                    .Subscribe(UpdateTurnLabel);
                UpdateTurnLabel(_turnService.State.CurrentValue);
            }
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
            _turnSubscription?.Dispose();
            _turnSubscription = null;
        }

        public void BindPlayerId(string playerId)
        {
            CurrentPlayerId = string.IsNullOrWhiteSpace(playerId) ? null : playerId;
            UpdateNameFromRoster();
            if (_turnService != null)
            {
                UpdateTurnLabel(_turnService.State.CurrentValue);
            }

            // Reset chest count display until we receive a specific value for this slot.
            SetChestCount(null);
        }

        public void SetChestCount(int? chestCount)
        {
            if (_chestCountLabel == null)
            {
                return;
            }

            var value = chestCount.GetValueOrDefault(0);
            _chestCountLabel.text = value.ToString();
        }

        private void SetName(string value)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = value ?? _emptyName;
            }
        }

        private void UpdateNameFromRoster()
        {
            if (_rosterService == null)
            {
                SetName(_emptyName);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentPlayerId))
            {
                SetName(_emptyName);
                return;
            }

            if (_rosterService.TryGetPlayer(CurrentPlayerId, out var info) &&
                !string.IsNullOrWhiteSpace(info.Name))
            {
                SetName(info.Name);
            }
            else
            {
                _rosterService.FetchPlayerAsync(CurrentPlayerId)
                    .ContinueWith(player => SetName(player != null ? player.Value.Name : _emptyName))
                    .Forget();
            }
        }

        private void UpdateTurnLabel(Modules.TurnSystem.Data.TurnState state)
        {
            if (_turnLabel == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentPlayerId) ||
                string.IsNullOrWhiteSpace(state.CurrentPlayerId))
            {
                _turnLabel.text = string.Empty;
                return;
            }

            if (!string.Equals(CurrentPlayerId, state.CurrentPlayerId, StringComparison.Ordinal))
            {
                _turnLabel.text = string.Empty;
                return;
            }

            _turnLabel.text = state.IsLocalTurn ? _yourTurnText : _opponentTurnText;
        }
    }
}
