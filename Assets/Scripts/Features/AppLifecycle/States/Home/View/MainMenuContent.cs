using System;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.Services;
using Features.LobbyImpl.View;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using ICVR.Window.Abstract;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.AppLifecycle.States.Home.View
{
    /// <summary>
    /// Simple home screen content that exposes entry points for creating or joining a lobby.
    /// </summary>
    public class MainMenuContent : AbstractContent
    {
        [Header("UI")]
        [SerializeField] private Button _joinGameButton;
        [SerializeField] private Button _createGameButton;

        [Header("Create Settings")]
        [SerializeField] private DeckType _deckType = DeckType.Short36;
        [SerializeField, Min(1)] private int _startingHand = 4;
        [SerializeField] private string _gameMode = "classic";

        [Header("Windows")]
        [SerializeField] private bool _openLobbyAfterCreate = true;

        private LobbyService _lobbyService;
        private LobbyFlowService _lobbyFlowService;
        private WindowSystem _windowSystem;
        private bool _isCreating;

        public override string Title => "Main Menu";

        [Inject]
        public void Construct(LobbyService lobbyService, LobbyFlowService lobbyFlowService, WindowSystem windowSystem)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _lobbyFlowService = lobbyFlowService ?? throw new ArgumentNullException(nameof(lobbyFlowService));
            _windowSystem = windowSystem;
        }

        private void Awake()
        {
            if (_createGameButton != null)
            {
                _createGameButton.onClick.AddListener(OnCreateClicked);
            }

            if (_joinGameButton != null)
            {
                _joinGameButton.onClick.AddListener(OnJoinClicked);
            }
        }

        private void OnDestroy()
        {
            if (_createGameButton != null)
            {
                _createGameButton.onClick.RemoveListener(OnCreateClicked);
            }

            if (_joinGameButton != null)
            {
                _joinGameButton.onClick.RemoveListener(OnJoinClicked);
            }
        }

        private void OnJoinClicked()
        {
            if (_windowSystem == null)
            {
                Debug.LogWarning("[MainMenu] Join window template is not configured.");
                return;
            }

            _windowSystem.ShowWindowAsync<JoinGameContent>(nameof(BlockerWindowTemplate)).Forget();
        }

        private void OnCreateClicked()
        {
            if (_isCreating || _lobbyService == null)
            {
                return;
            }

            CreateGameAsync().Forget();
        }

        private async UniTaskVoid CreateGameAsync()
        {
            _isCreating = true;
            SetCreateButtonInteractable(false);

            try
            {
                var options = new CreateGameOptions(
                    _deckType,
                    Mathf.Max(1, _startingHand),
                    string.IsNullOrWhiteSpace(_gameMode) ? "sprint1" : _gameMode.Trim());

                var result = await _lobbyService.CreateGameAsync(options, this.GetCancellationTokenOnDestroy());
                var joinOptions = new JoinGameOptions();
                await _lobbyService.JoinGameAsync(result.GameId, joinOptions, this.GetCancellationTokenOnDestroy());
                _lobbyService.UpdateData(new LobbyData
                {
                    GameId = result.GameId,
                    IsHost = true
                });

                if (_openLobbyAfterCreate)
                {
                    _lobbyFlowService.RequestEnter();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenu] Failed to create game: {ex.Message}");
            }
            finally
            {
                _isCreating = false;
                SetCreateButtonInteractable(true);
            }
        }

        private void SetCreateButtonInteractable(bool canInteract)
        {
            if (_createGameButton != null)
            {
                _createGameButton.interactable = canInteract;
            }
        }
    }
}
