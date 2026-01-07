using System;
using Features.AppLifecycle.Services;
using ICVR.Window.Abstract;
using Modules.Lobby.Services;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.LobbyImpl.View
{
    /// <summary>
    /// Displays lobby information (game id) and allows copying it while waiting for other players.
    /// </summary>
    public class LobbyContent : AbstractContent
    {
        [SerializeField] private TMP_Text _gameIdLabel;
        [SerializeField] private Button _copyButton;
        [SerializeField] private Button _closeButton;

        private LobbyService _lobbyService;
        private LobbyFlowService _lobbyFlowService;
        private CompositeDisposable _subscriptions;

        public override string Title => "Lobby";

        [Inject]
        public void Construct(LobbyService lobbyService, LobbyFlowService lobbyFlowService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _lobbyFlowService = lobbyFlowService ?? throw new ArgumentNullException(nameof(lobbyFlowService));
        }

        private void Awake()
        {
            if (_copyButton != null)
            {
                _copyButton.onClick.AddListener(OnCopyClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnEnable()
        {
            UpdateGameIdLabel();
            Subscribe();
        }

        private void OnDisable()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        private void OnDestroy()
        {
            if (_copyButton != null)
            {
                _copyButton.onClick.RemoveListener(OnCopyClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        private void Subscribe()
        {
            if (_lobbyService == null)
            {
                return;
            }

            _subscriptions = new CompositeDisposable();

            _lobbyService.State
                .Subscribe(_ => UpdateGameIdLabel())
                .AddTo(_subscriptions);

            _lobbyService.GameStarted
                .Subscribe(_ => Close())
                .AddTo(_subscriptions);
        }

        private void UpdateGameIdLabel()
        {
            if (_gameIdLabel == null || _lobbyService == null)
            {
                return;
            }

            var gameId = _lobbyService.StateContext.Data.GameId;
            _gameIdLabel.text = string.IsNullOrWhiteSpace(gameId) ? "-" : gameId;
        }

        private void OnCopyClicked()
        {
            var gameId = _lobbyService?.StateContext.Data.GameId;
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[Lobby] Nothing to copy, GameId is empty.");
                return;
            }

            GUIUtility.systemCopyBuffer = gameId;
        }

        private void OnCloseClicked()
        {
            _lobbyFlowService?.RequestExit();
            Close();
        }
    }
}
