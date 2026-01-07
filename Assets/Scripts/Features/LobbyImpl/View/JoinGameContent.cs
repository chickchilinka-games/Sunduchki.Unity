using System;
using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using Features.AppLifecycle.Services;
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
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _closeButton;

        private LobbyService _lobbyService;
        private LobbyFlowService _lobbyFlowService;
        private bool _isJoining;

        public override string Title => "Join Game";

        [Inject]
        public void Construct(LobbyService lobbyService, LobbyFlowService lobbyFlowService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _lobbyFlowService = lobbyFlowService ?? throw new ArgumentNullException(nameof(lobbyFlowService));
        }

        private void Awake()
        {
            if (_joinButton != null)
            {
                _joinButton.onClick.AddListener(OnJoinClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDestroy()
        {
            if (_joinButton != null)
            {
                _joinButton.onClick.RemoveListener(OnJoinClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
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

            _isJoining = true;
            SetJoinButtonInteractable(false);

            try
            {
                var options = new JoinGameOptions();
                await _lobbyService.JoinGameAsync(gameId, options, this.GetCancellationTokenOnDestroy());

                _lobbyFlowService.RequestEnter();

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
        private void SetJoinButtonInteractable(bool canInteract)
        {
            if (_joinButton != null)
            {
                _joinButton.interactable = canInteract;
            }
        }
    }
}
