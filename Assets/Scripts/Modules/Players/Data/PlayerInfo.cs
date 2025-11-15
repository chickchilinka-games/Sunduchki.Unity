namespace Modules.Players.Data
{
    public struct PlayerInfo
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsLocal { get; }

        public PlayerInfo(string id, string name, bool isLocal)
        {
            Id = id;
            Name = name;
            IsLocal = isLocal;
        }

        public PlayerInfo WithLocal(bool isLocal)
        {
            return new PlayerInfo(Id, Name, isLocal);
        }
    }
}
