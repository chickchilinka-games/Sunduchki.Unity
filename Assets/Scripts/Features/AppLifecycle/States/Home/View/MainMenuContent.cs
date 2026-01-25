using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.AppLifecycle.Services;
using Features.LobbyImpl.View;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using ICVR.Window.Abstract;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using TMPro;
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
        [SerializeField] private Button _findMatchButton;
        [SerializeField] private Button _cancelMatchButton;
        [SerializeField] private TMP_Text _matchmakingTimerText;
        [SerializeField] private TMP_Text _matchmakingStatusText;

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
        private bool _isSearching;
        private float _matchmakingStartedAt;
        private CancellationTokenSource _matchmakingCts;

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

            if (_findMatchButton != null)
            {
                _findMatchButton.onClick.AddListener(OnFindMatchClicked);
            }

            if (_cancelMatchButton != null)
            {
                _cancelMatchButton.onClick.AddListener(OnCancelMatchClicked);
            }
            
            SetSearching(false);
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

            if (_findMatchButton != null)
            {
                _findMatchButton.onClick.RemoveListener(OnFindMatchClicked);
            }

            if (_cancelMatchButton != null)
            {
                _cancelMatchButton.onClick.RemoveListener(OnCancelMatchClicked);
            }

            _matchmakingCts?.Cancel();
            _matchmakingCts?.Dispose();
        }

        private void Update()
        {
            if (_isSearching)
            {
                UpdateMatchmakingTimer();
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

        private void OnFindMatchClicked()
        {
            if (_isSearching || _lobbyService == null)
            {
                return;
            }

            _matchmakingCts?.Cancel();
            _matchmakingCts?.Dispose();
            _matchmakingCts = new CancellationTokenSource();
            StartMatchmakingAsync(_matchmakingCts.Token).Forget();
        }

        private void OnCancelMatchClicked()
        {
            if (!_isSearching)
            {
                return;
            }

            _matchmakingCts?.Cancel();
            _matchmakingCts?.Dispose();
            _matchmakingCts = null;
            SetSearching(false);
        }

        private async UniTaskVoid StartMatchmakingAsync(CancellationToken cancellationToken)
        {
            SetSearching(true);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                this.GetCancellationTokenOnDestroy());

            try
            {
                var options = new MatchmakingOptions(
                    _deckType,
                    Mathf.Max(1, _startingHand),
                    string.IsNullOrWhiteSpace(_gameMode) ? "classic" : _gameMode.Trim());

                var result = await _lobbyService.SearchMatchAsync(options, linkedCts.Token);
                _lobbyFlowService.RequestEnter();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[MainMenu] Matchmaking cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMenu] Matchmaking failed: {ex.Message}");
            }
            finally
            {
                SetSearching(false);
                _matchmakingCts?.Dispose();
                _matchmakingCts = null;
            }
        }

        private void SetCreateButtonInteractable(bool canInteract)
        {
            if (_createGameButton != null)
            {
                _createGameButton.interactable = canInteract;
            }
        }

        private void SetSearching(bool isSearching)
        {
            _isSearching = isSearching;
            _matchmakingStartedAt = isSearching ? Time.realtimeSinceStartup : 0f;

            if (_findMatchButton != null)
            {
                _findMatchButton.interactable = !isSearching;
            }

            if (_cancelMatchButton != null)
            {
                _cancelMatchButton.gameObject.SetActive(isSearching);
            }

            if (_matchmakingStatusText != null)
            {
                _matchmakingStatusText.text = isSearching ? "Searching..." : "Find game";
            }

            if (_matchmakingTimerText != null)
            {
                _matchmakingTimerText.text = isSearching ? "0.0s" : string.Empty;
            }
        }

        private void UpdateMatchmakingTimer()
        {
            if (_matchmakingTimerText == null)
            {
                return;
            }

            var elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _matchmakingStartedAt);
            _matchmakingTimerText.text = $"{elapsed:0.0}s";
        }
    }
}
