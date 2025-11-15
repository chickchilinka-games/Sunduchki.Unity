namespace Modules.Lobby.Data
{
    public struct LobbyGameEndedPayload
    {
        public object Result { get; }

        public LobbyGameEndedPayload(object result)
        {
            Result = result;
        }
    }
}
