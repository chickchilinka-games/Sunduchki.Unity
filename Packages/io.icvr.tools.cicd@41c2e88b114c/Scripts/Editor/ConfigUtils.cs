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

using System;
using System.Threading.Tasks;
using ICVR.Tools.Vault.Entities;
using ICVR.Tools.Vault.Services;
using UnityEngine;

namespace ICVR.Tools
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