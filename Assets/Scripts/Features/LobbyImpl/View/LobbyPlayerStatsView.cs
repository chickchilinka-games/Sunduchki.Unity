using Modules.Lobby.Data;
using TMPro;
using UnityEngine;

namespace Features.LobbyImpl.View
{
    /// <summary>
    /// Displays a lobby player's name and collected chest count.
    /// </summary>
    public class LobbyPlayerStatsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _chestCountLabel;
        [SerializeField] private string _emptyName = "—";

        public string CurrentPlayerId { get; private set; }

        public void BindPlayer(LobbyPlayerInfo? info)
        {
            if (info.HasValue)
            {
                CurrentPlayerId = info.Value.Id;
                SetName(string.IsNullOrWhiteSpace(info.Value.Name) ? _emptyName : info.Value.Name);
            }
            else
            {
                CurrentPlayerId = null;
                SetName(_emptyName);
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
    }
}
