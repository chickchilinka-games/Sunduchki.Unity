using System;
using Cysharp.Threading.Tasks;
using Modules.Players.Services;
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
        [SerializeField] private string _emptyName = "--";

        public string CurrentPlayerId { get; private set; }
        private PlayerRosterService _rosterService;
        private IDisposable _subscription;

        [Inject]
        public void Construct(PlayerRosterService rosterService)
        {
            _rosterService = rosterService ?? throw new ArgumentNullException(nameof(rosterService));
        }

        private void OnEnable()
        {
            if (_rosterService == null)
            {
                return;
            }

            _subscription = _rosterService.Players
                .Subscribe(_ => UpdateNameFromRoster());
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        public void BindPlayerId(string playerId)
        {
            CurrentPlayerId = string.IsNullOrWhiteSpace(playerId) ? null : playerId;
            UpdateNameFromRoster();

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
    }
}