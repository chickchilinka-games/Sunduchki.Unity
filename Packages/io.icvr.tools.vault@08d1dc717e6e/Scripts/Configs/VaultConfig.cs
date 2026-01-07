// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using UnityEngine;

namespace ICVR.Tools.Vault.Data
{
    internal class VaultConfig : ScriptableObject
    {
        [SerializeField]
        private string _vaultUrl;
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