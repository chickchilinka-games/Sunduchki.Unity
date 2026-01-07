namespace Modules.Lobby.Data
{
    public struct LobbyPlayerInfo
    {
        public string Id { get; }
        public bool IsLocal { get; }

        public LobbyPlayerInfo(string id, bool isLocal)
        {
            Id = id;
            IsLocal = isLocal;
        }
    }
}
