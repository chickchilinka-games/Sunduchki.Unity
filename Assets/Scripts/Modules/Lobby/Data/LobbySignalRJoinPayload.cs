namespace Modules.Lobby.Data
{
    public struct LobbySignalRJoinPayload
    {
        public string GameId { get; }
        public string PlayerId { get; }

        public LobbySignalRJoinPayload(string gameId, string playerId)
        {
            GameId = gameId;
            PlayerId = playerId;
        }
    }
}
