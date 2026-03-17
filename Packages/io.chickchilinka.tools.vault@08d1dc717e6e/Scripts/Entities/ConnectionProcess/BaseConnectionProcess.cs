// // Chickchilinka CONFIDENTIAL
// // __________________
// //
// // [2016] - [2024] Chickchilinka LLC
// // All Rights Reserved.
// //
// // NOTICE:  All information contained herein is, and remains
// // the property of Chickchilinka LLC and its suppliers,
// // if any.  The intellectual and technical concepts contained
// // herein are proprietary to Chickchilinka LLC
// // and its suppliers and may be covered by U.S. and Foreign Patents,
// // patents in process, and are protected by trade secret or copyright law.
// // Dissemination of this information or reproduction of this material
// // is strictly forbidden unless prior written permission is obtained
// // from Chickchilinka LLC.

using System.Collections.Generic;
using System.Threading;
using Chickchilinka.Tools.Vault.Data;
using Chickchilinka.Tools.Vault.Interfaces;
using Chickchilinka.Tools.Vault.Utils;

namespace Chickchilinka.Tools.Vault.Entities
{
    internal abstract class BaseConnectionProcess : VaultDispatcher, IConnectionProcess
    {
        protected string LoginUrl => VaultConfig.VaultUrl + "/v1/auth/" + AuthType.ToString().ToLower() + "/login";
        public abstract AuthType AuthType { get; }

        public bool Connect(string credentials, CancellationToken cancellationToken)
        {
            var token = GetToken(credentials, cancellationToken);

            if (string.IsNullOrEmpty(token)) return false;

            Initialize(token);

            return true;
        }

        public void Disconnect()
        {
            Dispose();
        }

        public string GetRawData(VaultConnectionSettings connectionSettings, CancellationToken cancellationToken)
        {
            var response = GetData(connectionSettings.Environment.ToString(),
                connectionSettings.ProjectName,
                connectionSettings.ClientName,
                cancellationToken);
            var rawData = SerializationUtils.Deserialize<PullResponseData>(response);

            return rawData?.data?.data.ToString();
        }

        protected abstract string GetToken(string credentials, CancellationToken cancellationToken);

        protected string Post(string url, Dictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            return WebUtils.Post(url, parameters, cancellationToken);
        }
    }
}