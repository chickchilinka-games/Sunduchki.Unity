namespace Modules.Lobby.Data
{
    public struct LobbyPlayerInfo
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsLocal { get; }

        public LobbyPlayerInfo(string id, string name, bool isLocal)
        {
            Id = id;
            Name = name;
            IsLocal = isLocal;
        }
    }
}
