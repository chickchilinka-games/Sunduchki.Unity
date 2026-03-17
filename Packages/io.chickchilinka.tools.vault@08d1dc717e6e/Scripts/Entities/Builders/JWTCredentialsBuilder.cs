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

using Chickchilinka.Tools.Vault.Data;
using Chickchilinka.Tools.Vault.Utils;

namespace Chickchilinka.Tools.Vault.Entities
{
    public class JWTCredentialsBuilder
    {
        private readonly JWTCredentials _credentials;

        public JWTCredentialsBuilder(string jwt, string role)
        {
            _credentials = new JWTCredentials(AuthType.JWT, jwt, role);
        }

        public string Build()
        {
            if (_credentials == null
                || string.IsNullOrEmpty(_credentials.Jwt)
                || string.IsNullOrEmpty(_credentials.Role))
                return null;

            return SerializationUtils.Serialize(_credentials);
        }
    }
}