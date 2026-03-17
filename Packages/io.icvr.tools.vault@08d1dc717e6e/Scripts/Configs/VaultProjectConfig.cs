using UnityEngine;

namespace ICVR.Tools.Vault.Data
{
    public class VaultProjectConfig : ScriptableObject
    {
        [SerializeField] private string _projectName;

        [SerializeField] private string _clientName;

        public string ProjectName => _projectName;
        public string ClientName => _clientName;
    }
}