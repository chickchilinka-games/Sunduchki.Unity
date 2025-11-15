namespace Modules.Lobby.Data
{
    public struct CreateGameResult
    {
        public string GameId { get; }

        public CreateGameResult(string gameId)
        {
            GameId = gameId;
        }
    }
}
