using System;

namespace Modules.Lobby.Data
{
    public struct LobbyState
    {
        public LobbyStatus Status { get; private set; }
        public bool Started { get; private set; }
        public string ErrorMessage { get; private set; }
        public int? DeckCount { get; private set; }
        public int? TotalCards { get; private set; }
        public GameEndedResultDto Result { get; private set; }
        public string GameEndedReason { get; private set; }

        public LobbyState(
            LobbyStatus status,
            bool started,
            string errorMessage,
            int? deckCount,
            int? totalCards,
            GameEndedResultDto result,
            string gameEndedReason)
        {
            Status = status;
            Started = started;
            ErrorMessage = errorMessage;
            DeckCount = deckCount;
            TotalCards = totalCards;
            Result = result;
            GameEndedReason = gameEndedReason;
        }

        public static LobbyState Default => new(
            LobbyStatus.Idle,
            false,
            null,
            null,
            null,
            null,
            null);

        public LobbyState WithStatus(LobbyStatus status)
        {
            var copy = this;
            copy.Status = status;
            return copy;
        }

        public LobbyState WithStarted(bool started)
        {
            var copy = this;
            copy.Started = started;
            return copy;
        }

        public LobbyState WithError(string errorMessage)
        {
            var copy = this;
            copy.ErrorMessage = errorMessage;
            return copy;
        }

        public LobbyState WithDeckInfo(int? deckCount, int? totalCards)
        {
            var copy = this;
            copy.DeckCount = deckCount;
            copy.TotalCards = totalCards;
            return copy;
        }

        public LobbyState WithResult(GameEndedResultDto result)
        {
            var copy = this;
            copy.Result = result;
            return copy;
        }

        public LobbyState WithGameEndedReason(string reason)
        {
            var copy = this;
            copy.GameEndedReason = reason;
            return copy;
        }

        public LobbyState ClearError()
        {
            var copy = this;
            copy.ErrorMessage = null;
            return copy;
        }

        public LobbyState With(Action<LobbyStateBuilder> patch)
        {
            var builder = new LobbyStateBuilder(this);
            patch?.Invoke(builder);
            return builder.Build();
        }
    }
}
