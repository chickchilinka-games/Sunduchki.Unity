using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Chickchilinka.Tools.Vault.Data;
using Chickchilinka.Tools.Vault.Utils;
using UnityEditor;
using UnityEngine;

namespace Chickchilinka.Tools.Vault.Entities
{
    internal class VaultDispatcher
    {
        private const string TokenHeaderKey = "X-Vault-Token";

        protected VaultDispatcher()
        {
            VaultConfig = FindConfig();
        }

        protected VaultConfig VaultConfig { get; }

        ~VaultDispatcher()
        {
            Dispose();
        }

        protected void Initialize(string vaultToken)
        {
            VaultConfig.SetToken(vaultToken);
        }

        protected void Dispose()
        {
            VaultConfig.ResetToken();
        }

        protected string GetData(string environment,
            string projectName,
            string clientName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(VaultConfig.GetToken()))
            {
                Debug.LogError("[Vault] VaultDispatcher isn't initialized!");

                return null;
            }

            var vaultPath = clientName + "/data/" + projectName + "/" + environment;
            var url = VaultConfig.VaultUrl + "/v1/" + vaultPath;

            var serializedData = WebUtils.Get(url,
                cancellationToken,
                new Dictionary<string, string> { { TokenHeaderKey, VaultConfig.GetToken() } });

            return serializedData;
        }

        private VaultConfig FindConfig()
        {
            var assetGuid = AssetDatabase.FindAssets("t:VaultConfig");

            if (assetGuid == null || !assetGuid.Any())
            {
                Debug.LogError("[Vault] Vault config not found!");

                return null;
            }

            if (assetGuid.Length > 1)
            {
                Debug.LogError("[Vault] There were found several Vault configs, but expected one!");

                return null;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid.First());
            var config = AssetDatabase.LoadAssetAtPath<VaultConfig>(assetPath);

            return config;
        }
    }
}