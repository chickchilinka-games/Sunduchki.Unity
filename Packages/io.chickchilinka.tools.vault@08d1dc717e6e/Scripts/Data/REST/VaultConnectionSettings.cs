namespace Chickchilinka.Tools.Vault.Data
{
    internal class VaultConnectionSettings
    {
        public VaultConnectionSettings(VaultEnvironment environment, string projectName, string clientName)
        {
            Environment = environment;
            ProjectName = projectName;
            ClientName = clientName;
        }

        public VaultEnvironment Environment { get; }
        public string ProjectName { get; }
        public string ClientName { get; }
    }
}