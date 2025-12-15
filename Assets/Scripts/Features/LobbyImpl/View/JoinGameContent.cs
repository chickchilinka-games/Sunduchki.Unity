using System;
using Cysharp.Threading.Tasks;
using ICVR.Window;
using ICVR.Window.Abstract;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.LobbyImpl.View
{
    /// <summary>
    /// Window that allows a player to input a lobby identifier and join it.
    /// </summary>
    public class JoinGameContent : AbstractContent
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField _gameIdInput;
        [SerializeField] private TMP_InputField _playerNameInput;
        [SerializeField] private Button _joinButton;

        [Header("Auth")]
        [SerializeField, TextArea] private string _authToken = string.Empty;

        [Header("Windows")]
        [SerializeField] private string _lobbyTemplateId = "DefaultWindow";
        [SerializeField] private bool _openLobbyOnSuccess = true;

        private LobbyService _lobbyService;
        private WindowSystem _windowSystem;
        private bool _isJoining;

        public override string Title => "Join Game";

        [Inject]
        public void Construct(LobbyService lobbyService, WindowSystem windowSystem)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _windowSystem = windowSystem;
        }

        private void Awake()
        {
            if (_joinButton != null)
            {
                _joinButton.onClick.AddListener(OnJoinClicked);
            }
        }

        private void OnDestroy()
        {
            if (_joinButton != null)
            {
                _joinButton.onClick.RemoveListener(OnJoinClicked);
            }
        }

        private void OnJoinClicked()
        {
            if (_isJoining || _lobbyService == null)
            {
                return;
            }

            JoinAsync().Forget();
        }

        private async UniTaskVoid JoinAsync()
        {
            var gameId = _gameIdInput != null ? _gameIdInput.text?.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[JoinGame] GameId is required.");
                return;
            }

            var token = ResolveAuthToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                Debug.LogWarning("[JoinGame] Auth token is required.");
                return;
            }

            var playerName = _playerNameInput != null ? _playerNameInput.text?.Trim() : null;

            _isJoining = true;
            SetJoinButtonInteractable(false);

            try
            {
                var options = new JoinGameOptions(token, playerName);
                await _lobbyService.JoinGameAsync(gameId, options, this.GetCancellationTokenOnDestroy());

                if (_openLobbyOnSuccess && _windowSystem != null && !string.IsNullOrWhiteSpace(_lobbyTemplateId))
                {
                    await _windowSystem.ShowWindowAsync<LobbyContent>(_lobbyTemplateId);
                }

                Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JoinGame] Failed to join lobby '{gameId}': {ex.Message}");
            }
            finally
            {
                _isJoining = false;
                SetJoinButtonInteractable(true);
            }
        }

        private string ResolveAuthToken()
        {
            return _authToken?.Trim();
        }

        private void SetJoinButtonInteractable(bool canInteract)
        {
            if (_joinButton != null)
            {
                _joinButton.interactable = canInteract;
            }
        }
    }
}
