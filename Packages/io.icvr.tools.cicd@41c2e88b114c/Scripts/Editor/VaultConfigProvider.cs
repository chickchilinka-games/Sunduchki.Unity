

using ICVR.Tools.Vault.Services;

namespace ICVR.Tools
{
    public class VaultConfigProvider : IConfigProvider
    {
        private readonly VaultService _vaultService;

        public VaultConfigProvider(VaultService vaultService)
        {
            _vaultService = vaultService;
        }
        
        public TData GetData<TData>() where TData : class
        {
            return _vaultService.GetData<TData>();
        }

        public string GetRawData()
        {
            return _vaultService.GetRawData();
        }

        public bool CreateFileFromProperty(string propertyName, string resultFilePath)
        {
            return _vaultService.CreateFileFromProperty(propertyName, resultFilePath);
        }
    }
}