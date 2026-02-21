using System;
using Features.PlayerHandSystemImpl.Storage;
using Modules.Lobby.Data;
using R3;

namespace Features.PlayerHandSystemImpl.Presenters
{
    public sealed class PlayerHandSetCompletionPresenter : IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly RankStackViewModelStore _store;
        private readonly Subject<string> _setCompleted = new();
        private static readonly TimeSpan SuppressRankDuration = TimeSpan.FromSeconds(1.5);

        public PlayerHandSetCompletionPresenter(
            PlayerHandPresentationContext context,
            RankStackViewModelStore store)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Observable<string> SetCompleted => _setCompleted;

        public void Dispose()
        {
            _setCompleted.Dispose();
        }

        public void OnChestUpdated(LobbyChestUpdatedPayload payload)
        {
            if (!_context.IsActive.CurrentValue)
            {
                return;
            }

            var localPlayerId = _context.LocalPlayerId.CurrentValue;
            if (payload.PlayerId == null ||
                string.IsNullOrWhiteSpace(localPlayerId) ||
                !string.Equals(payload.PlayerId, localPlayerId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var rankKey = NormalizeRank(payload.Rank);
            _store.SuppressRank(rankKey, SuppressRankDuration);
            if (_store.TryGet(rankKey, out var viewModel))
            {
                viewModel.MarkSetCompleted();
                _setCompleted.OnNext(rankKey);
                // WebGL: events arrive from different SignalR connections and sometimes
                // hand snapshot removal lags behind. SetCompleted is authoritative, so
                // force-remove the stack from VM store to avoid stale rank views.
                _store.Remove(rankKey, out _);
                return;
            }

            _setCompleted.OnNext(rankKey);
        }

        private static string NormalizeRank(string rank)
        {
            return string.IsNullOrWhiteSpace(rank)
                ? "unknown"
                : rank.Trim().ToLowerInvariant();
        }
    }
}
