using System;
using System.Collections.Generic;
using ICVR.Window.Abstract;
using Modules.Lobby.Data;
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
        [SerializeField] private LobbyPlayerStatsView _localPlayerView;
        [SerializeField] private LobbyPlayerStatsView _opponentPlayerView;

        private LobbyService _lobbyService;
        private CompositeDisposable _subscriptions;
        private readonly Dictionary<string, int> _chestCounts = new(StringComparer.Ordinal);

        public override string Title => "Lobby";

        [Inject]
        public void Construct(LobbyService lobbyService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        private void Awake()
        {
            if (_copyButton != null)
            {
                _copyButton.onClick.AddListener(OnCopyClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        private void OnEnable()
        {
            UpdateGameIdLabel();
            UpdatePlayerViews(_lobbyService?.Players.CurrentValue);
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
                _closeButton.onClick.RemoveListener(Close);
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

            _lobbyService.Players
                .Subscribe(UpdatePlayerViews)
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

            var gameId = _lobbyService.StateContext.Config.GameId;
            _gameIdLabel.text = string.IsNullOrWhiteSpace(gameId) ? "-" : gameId;
        }

        private void UpdatePlayerViews(IReadOnlyList<LobbyPlayerInfo> players)
        {
            _localPlayerView?.BindPlayer(FindPlayer(players, true));
            _opponentPlayerView?.BindPlayer(FindPlayer(players, false));
            ApplyChestCounts();
        }

        private static LobbyPlayerInfo? FindPlayer(IReadOnlyList<LobbyPlayerInfo> players, bool isLocal)
        {
            if (players == null)
            {
                return null;
            }

            for (var index = 0; index < players.Count; index++)
            {
                var candidate = players[index];
                if (candidate.IsLocal == isLocal)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ApplyChestCounts()
        {
            ApplyChestCountForView(_localPlayerView);
            ApplyChestCountForView(_opponentPlayerView);
        }

        private void ApplyChestCountForView(LobbyPlayerStatsView view)
        {
            if (view == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(view.CurrentPlayerId) &&
                _chestCounts.TryGetValue(view.CurrentPlayerId, out var value))
            {
                view.SetChestCount(value);
            }
            else
            {
                view.SetChestCount(0);
            }
        }

        /// <summary>
        /// Updates the chest counter for a specific player. Can be called by external presenters.
        /// </summary>
        public void SetChestCount(string playerId, int chestCount)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var normalized = Mathf.Max(0, chestCount);
            _chestCounts[playerId] = normalized;
            ApplyChestCount(playerId, normalized);
        }

        private void ApplyChestCount(string playerId, int value)
        {
            if (_localPlayerView != null && _localPlayerView.CurrentPlayerId == playerId)
            {
                _localPlayerView.SetChestCount(value);
            }

            if (_opponentPlayerView != null && _opponentPlayerView.CurrentPlayerId == playerId)
            {
                _opponentPlayerView.SetChestCount(value);
            }
        }

        private void OnCopyClicked()
        {
            var gameId = _lobbyService?.StateContext.Config.GameId;
            if (string.IsNullOrWhiteSpace(gameId))
            {
                Debug.LogWarning("[Lobby] Nothing to copy, GameId is empty.");
                return;
            }

            GUIUtility.systemCopyBuffer = gameId;
        }
    }
}
