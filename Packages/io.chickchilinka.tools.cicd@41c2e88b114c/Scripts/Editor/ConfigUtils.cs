

using System;
using System.Threading.Tasks;
using Chickchilinka.Tools.Vault.Entities;
using Chickchilinka.Tools.Vault.Services;
using UnityEngine;

namespace Chickchilinka.Tools
{
    public class ConfigUtils
    {
        public static void TryConnectConfigProvider(string projectName,
                                                    string clientName,
                                                    string env,
                                                    Action<IConfigProvider> callback)
        {
            var vaultService = new VaultService();
            var jwt = Environment.GetEnvironmentVariable("CI_JOB_JWT");
            var role = $"{clientName.ToLower()}-{projectName.ToLower()}-{env.ToLower()}";
            
            if (!string.IsNullOrEmpty(jwt) && !string.IsNullOrEmpty(role))
            {
                var credentials = new JWTCredentialsBuilder(jwt, role).Build();
                var isConnectionSuccess = vaultService.Connect(credentials, env, projectName, clientName);

                if (isConnectionSuccess)
                {
                    callback?.Invoke(new VaultConfigProvider(vaultService));
                }
                else
                {
                    Debug.LogError("Build failed! There is no connection with Vault!");

                    callback?.Invoke(null);
                }
            }
            else
            {
                vaultService.ConnectUI(OnResult, env, projectName, clientName);

                void OnResult(bool success)
                {
                    if (success)
                    {
                        callback?.Invoke(new VaultConfigProvider(vaultService));
                    }
                    else
                    {
                        Debug.LogError("Build failed! There is no connection with Vault!");

                        callback?.Invoke(null);
                    }
                }
            }
        }
    }
}