using System;
using Features.PlayerHandSystemImpl.Storage;
using Modules.Lobby.Data;
using R3;

namespace Features.PlayerHandSystemImpl.Presenters
{
    public sealed class PlayerHandSetCompletionPresenter : IDisposable
    {
        private readonly PlayerHandPresentationContext _context;
        private readonly Subject<string> _setCompleted = new();

        public PlayerHandSetCompletionPresenter(
            PlayerHandPresentationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
