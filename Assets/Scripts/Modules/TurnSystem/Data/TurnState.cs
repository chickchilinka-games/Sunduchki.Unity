namespace Modules.TurnSystem.Data
{
    public struct TurnState
    {
        public string CurrentPlayerId { get; }
        public string PreviousPlayerId { get; }
        public int TurnIndex { get; }
        public bool IsLocalTurn { get; }

        public static TurnState Default => new(string.Empty, string.Empty, 0, false);

        public TurnState(string currentPlayerId, string previousPlayerId, int turnIndex, bool isLocalTurn)
        {
            CurrentPlayerId = currentPlayerId;
            PreviousPlayerId = previousPlayerId;
            TurnIndex = turnIndex;
            IsLocalTurn = isLocalTurn;
        }

        public TurnState WithCurrent(string playerId, bool isLocalTurn)
        {
            return new TurnState(playerId ?? string.Empty, CurrentPlayerId, TurnIndex + 1, isLocalTurn);
        }

        public TurnState Reset()
        {
            return Default;
        }
    }
}
