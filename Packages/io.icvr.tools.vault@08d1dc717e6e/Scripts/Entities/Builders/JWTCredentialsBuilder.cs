// // ICVR CONFIDENTIAL
// // __________________
// //
// // [2016] - [2024] ICVR LLC
// // All Rights Reserved.
// //
// // NOTICE:  All information contained herein is, and remains
// // the property of ICVR LLC and its suppliers,
// // if any.  The intellectual and technical concepts contained
// // herein are proprietary to ICVR LLC
// // and its suppliers and may be covered by U.S. and Foreign Patents,
// // patents in process, and are protected by trade secret or copyright law.
// // Dissemination of this information or reproduction of this material
// // is strictly forbidden unless prior written permission is obtained
// // from ICVR LLC.

using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Utils;

namespace ICVR.Tools.Vault.Entities
{
    public class JWTCredentialsBuilder
    {
        private JWTCredentials _credentials;
        
        public JWTCredentialsBuilder(string jwt, string role)
        {
            _credentials = new JWTCredentials(AuthType.JWT, jwt, role);
        }

        public string Build()
        {
            if (_credentials == null
                || string.IsNullOrEmpty(_credentials.Jwt)
                || string.IsNullOrEmpty(_credentials.Role))
            {
                return null;
            }

            return SerializationUtils.Serialize(_credentials);
        }
    }
}