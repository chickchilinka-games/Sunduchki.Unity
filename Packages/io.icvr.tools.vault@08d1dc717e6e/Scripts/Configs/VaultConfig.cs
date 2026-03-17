using UnityEngine;

namespace ICVR.Tools.Vault.Data
{
    internal class VaultConfig : ScriptableObject
    {
        [SerializeField] private string _vaultUrl;

        private string _vaultToken;

        public string VaultUrl => _vaultUrl;

        public void SetToken(string token)
        {
            _vaultToken = token;
        }

        public string GetToken()
        {
            return _vaultToken;
        }

        public void ResetToken()
        {
            _vaultToken = string.Empty;
        }
    }
}