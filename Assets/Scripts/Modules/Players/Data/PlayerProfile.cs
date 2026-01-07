namespace Modules.Players.Data
{
    public struct PlayerProfile
    {
        public string DisplayName { get; }
        public string PhotoUrl { get; }

        public PlayerProfile(string displayName, string photoUrl)
        {
            DisplayName = displayName;
            PhotoUrl = photoUrl;
        }
    }
}
