namespace Modules.Lobby.Data
{
    public readonly struct LobbyGameEndedPayload
    {
        public GameEndedResultDto Result { get; }
        public string Reason { get; }

        public LobbyGameEndedPayload(GameEndedResultDto result, string reason)
        {
            Result = result;
            Reason = reason;
        }
    }
}
