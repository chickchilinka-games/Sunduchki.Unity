namespace Modules.Profiles.Data
{
    public struct ProfileInfo
    {
        public string DisplayName { get; }
        public string PhotoUrl { get; }

        public ProfileInfo(string displayName, string photoUrl)
        {
            DisplayName = displayName;
            PhotoUrl = photoUrl;
        }
    }
}
