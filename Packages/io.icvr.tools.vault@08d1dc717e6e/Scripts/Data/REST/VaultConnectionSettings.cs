namespace ICVR.Tools.Vault.Data
{
    internal class VaultConnectionSettings
    {
        public VaultEnvironment Environment { get; }
        public string ProjectName { get; }
        public string ClientName { get; }

        public VaultConnectionSettings(VaultEnvironment environment, string projectName, string clientName)
        {
            Environment = environment;
            ProjectName = projectName;
            ClientName = clientName;
        }
    }
}