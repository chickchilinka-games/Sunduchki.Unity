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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Utils;
using UnityEditor;
using UnityEngine;

namespace ICVR.Tools.Vault.Entities
{
    internal class VaultDispatcher
    {
        private const string TokenHeaderKey = "X-Vault-Token";
        
        private readonly VaultConfig _config;

        protected VaultConfig VaultConfig => _config;

        protected VaultDispatcher()
        {
            _config = FindConfig();
        }

        ~VaultDispatcher()
        {
            Dispose();
        }

        protected void Initialize(string vaultToken)
        {
            _config.SetToken(vaultToken);
        }

        protected void Dispose()
        {
            _config.ResetToken();
        }
        
        protected string GetData(string environment, 
                                            string projectName, 
                                            string clientName,
                                            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_config.GetToken()))
            {
                Debug.LogError($"[Vault] VaultDispatcher isn't initialized!");
                
                return null;
            }
            
            var vaultPath = clientName + "/data/" + projectName + "/" + environment;
            var url = _config.VaultUrl + "/v1/" + vaultPath;
            
            var serializedData = WebUtils.Get(url, 
                                            cancellationToken,
                                            new Dictionary<string, string>(){{TokenHeaderKey, _config.GetToken()}});
            
            return serializedData;
        }
        
        private VaultConfig FindConfig()
        {
            var assetGuid = AssetDatabase.FindAssets("t:VaultConfig");

            if (assetGuid == null || !assetGuid.Any())
            {
                Debug.LogError($"[Vault] Vault config not found!");

                return null;
            }

            if (assetGuid.Length > 1)
            {
                Debug.LogError($"[Vault] There were found several Vault configs, but expected one!");

                return null;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid.First());
            var config = AssetDatabase.LoadAssetAtPath<VaultConfig>(assetPath);

            return config;
        }
    }
}