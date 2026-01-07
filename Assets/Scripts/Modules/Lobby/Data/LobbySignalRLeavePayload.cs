namespace Modules.Lobby.Data
{
    public readonly struct LobbySignalRLeavePayload
    {
        public string GameId { get; }
        public string PlayerId { get; }

        public LobbySignalRLeavePayload(string gameId, string playerId)
        {
            GameId = gameId;
            PlayerId = playerId;
        }
    }
}
