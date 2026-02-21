using System;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using Modules.PlayerHand.Interfaces;
using Modules.PlayerHand.Services;
using R3;
using Zenject;

namespace Modules.PlayerHand.Rules
{
    internal sealed class TrackPlayerHandEventsRule : IInitializable, IDisposable
    {
        private readonly LobbyService _lobbyService;
        private readonly IPlayerHandEventSource _client;
        private readonly PlayerHandInternalService _internalService;
        private readonly CompositeDisposable _subscriptions = new();
        private bool _keepStateOnReset;
        private string _trackedGameId = string.Empty;
        private string _trackedPlayerId = string.Empty;

        public TrackPlayerHandEventsRule(
            LobbyService lobbyService,
            IPlayerHandEventSource client,
            PlayerHandInternalService internalService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _internalService = internalService ?? throw new ArgumentNullException(nameof(internalService));
        }

        public void Initialize()
        {
            _client.StandardSnapshot.Subscribe(evt =>
                {
                    var playerId = ResolvePlayerId(evt.PlayerId);
                    _internalService.ApplyStandardSnapshot(playerId, evt.Cards, evt.Revision);
                })
                .AddTo(_subscriptions);

            _client.StandardCardAdded.Subscribe(evt =>
                {
                    var playerId = ResolvePlayerId(evt.PlayerId);
                    _internalService.AddStandardCard(playerId, evt.Card, evt.EventSeq);
                })
                .AddTo(_subscriptions);

            _client.StandardCardRemoved.Subscribe(evt =>
                {
                    var playerId = ResolvePlayerId(evt.PlayerId);
                    _internalService.RemoveStandardCard(
                        playerId,
                        evt.Card,
                        evt.EventSeq,
                        evt.CompletedSet ? evt.CompletedSetRank : string.Empty);
                })
                .AddTo(_subscriptions);

            _client.BonusCardAdded.Subscribe(evt =>
                {
                    var playerId = ResolvePlayerId(evt.PlayerId);
                    _internalService.AddBonusCard(playerId, evt.Card);
                })
                .AddTo(_subscriptions);

            _client.BonusCardRemoved.Subscribe(evt =>
                {
                    var playerId = ResolvePlayerId(evt.PlayerId);
                    _internalService.NotifyBonusUsed(playerId, evt.Card);
                    _internalService.RemoveBonusCard(playerId, evt.Card);
                })
                .AddTo(_subscriptions);

            _client.CardsReceived.Subscribe(evt =>
                {
                    var playerId = ResolvePlayerId(evt.PlayerId);
                    _internalService.NotifyCardsReceived(
                        playerId,
                        evt.Source,
                        evt.StandardCards,
                        evt.BonusCards,
                        evt.EventSeq,
                        evt.CompletedSet,
                        evt.CompletedSetRank);

                    if (evt.CompletedSet && !string.IsNullOrWhiteSpace(evt.CompletedSetRank))
                    {
                        ApplySetCompletedDeferred(playerId, evt.CompletedSetRank, evt.EventSeq).Forget();
                    }
                })
                .AddTo(_subscriptions);

            _lobbyService.State.Subscribe(state =>
                {
                    TrackLobbySession();

                    if (state.Status == LobbyStatus.Ended)
                    {
                        _keepStateOnReset = true;
                        return;
                    }

                    if (state.Status != LobbyStatus.Idle || state.Started)
                    {
                        return;
                    }

                    if (_keepStateOnReset)
                    {
                        _keepStateOnReset = false;
                        return;
                    }

                    _internalService.ResetAll();
                    _trackedGameId = string.Empty;
                    _trackedPlayerId = string.Empty;
                })
                .AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
            _internalService.ResetAll();
            _trackedGameId = string.Empty;
            _trackedPlayerId = string.Empty;
        }

        private void TrackLobbySession()
        {
            if (!_lobbyService.TryGetSession(out var gameId, out var playerId))
            {
                return;
            }

            var changed = !string.Equals(_trackedGameId, gameId, StringComparison.Ordinal) ||
                          !string.Equals(_trackedPlayerId, playerId, StringComparison.Ordinal);
            if (!changed)
            {
                return;
            }

            _trackedGameId = gameId;
            _trackedPlayerId = playerId;
            _internalService.ResetAll();
        }

        private string ResolvePlayerId(string playerId)
        {
            TrackLobbySession();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(_trackedPlayerId) &&
                string.Equals(playerId, _trackedPlayerId, StringComparison.OrdinalIgnoreCase))
            {
                return _trackedPlayerId;
            }

            return playerId;
        }

        private async UniTaskVoid ApplySetCompletedDeferred(string playerId, string rank, long eventSeq)
        {
            // Let CardsReceived presentation enqueue receive animations first.
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            _internalService.ApplySetCompleted(playerId, rank, eventSeq);
        }
    }
}

