namespace Modules.Lobby.Data
{
    public readonly struct LobbySession
    {
        public string GameId { get; }
        public string PlayerId { get; }

        public LobbySession(string gameId, string playerId)
        {
            GameId = gameId ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
        }
    }
}
