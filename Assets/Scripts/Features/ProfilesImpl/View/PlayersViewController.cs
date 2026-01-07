using System;
using System.Collections.Generic;
using Features.LobbyImpl.View;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using R3;
using UnityEngine;
using Zenject;

namespace Features.ProfilesImpl.View
{
    public class PlayersViewController : MonoBehaviour, IDisposable
    {
        [SerializeField] private LobbyPlayerStatsView _localPlayerView;
        [SerializeField] private LobbyPlayerStatsView _opponentPlayerView;

        private LobbyService _lobbyService;
        private CompositeDisposable _subscriptions;
        private readonly Dictionary<string, int> _chestCounts = new(StringComparer.Ordinal);

        [Inject]
        public void Construct(LobbyService lobbyService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
        }

        private void OnEnable()
        {
            UpdatePlayerViews(_lobbyService?.Players.CurrentValue);
            Subscribe();
        }

        private void OnDisable()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        public void Dispose()
        {
            _subscriptions?.Dispose();
        }

        private void Subscribe()
        {
            if (_lobbyService == null)
            {
                return;
            }

            _subscriptions = new CompositeDisposable();

            _lobbyService.Players
                .Subscribe(UpdatePlayerViews)
                .AddTo(_subscriptions);

            _lobbyService.ChestUpdated
                .Subscribe(payload => SetChestCount(payload.PlayerId, payload.ChestCount))
                .AddTo(_subscriptions);
        }

        private void UpdatePlayerViews(IReadOnlyList<LobbyPlayerInfo> players)
        {
            _localPlayerView?.BindPlayerId(FindPlayerId(players, true));
            _opponentPlayerView?.BindPlayerId(FindPlayerId(players, false));
            ApplyChestCounts();
        }

        private static string FindPlayerId(IReadOnlyList<LobbyPlayerInfo> players, bool isLocal)
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
                    return candidate.Id;
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
    }
}
