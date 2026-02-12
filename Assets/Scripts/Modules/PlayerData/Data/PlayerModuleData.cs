namespace Modules.PlayerData.Data
{
    public class PlayerModuleData
    {
        public int RemoteVersion { get; set; }
        public bool IsPendingUpload { get; set; }
        public int BaseVersion { get; set; }
        public string ModuleName { get; set; }
        public string Payload { get; set; }
    }
}
