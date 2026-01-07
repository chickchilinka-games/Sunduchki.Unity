namespace Modules.Players.Data
{
    public struct PlayerInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string PhotoUrl { get; }
        public bool IsLocal { get; }

        public PlayerInfo(string id, string name, string photoUrl, bool isLocal)
        {
            Id = id;
            Name = name;
            PhotoUrl = photoUrl;
            IsLocal = isLocal;
        }

        public PlayerInfo WithLocal(bool isLocal)
        {
            return new PlayerInfo(Id, Name, PhotoUrl, isLocal);
        }
    }
}
