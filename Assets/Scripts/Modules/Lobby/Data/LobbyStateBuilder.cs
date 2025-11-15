namespace Modules.Lobby.Data
{
    public sealed class LobbyStateBuilder
    {
        public LobbyStatus Status { get; set; }
        public bool Started { get; set; }
        public string ErrorMessage { get; set; }
        public int? DeckCount { get; set; }
        public int? TotalCards { get; set; }
        public object Result { get; set; }

        public LobbyStateBuilder(LobbyState origin)
        {
            Status = origin.Status;
            Started = origin.Started;
            ErrorMessage = origin.ErrorMessage;
            DeckCount = origin.DeckCount;
            TotalCards = origin.TotalCards;
            Result = origin.Result;
        }

        public LobbyState Build()
        {
            return new LobbyState(Status, Started, ErrorMessage, DeckCount, TotalCards, Result);
        }
    }
}
