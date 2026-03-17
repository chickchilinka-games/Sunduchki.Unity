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

namespace Chickchilinka.Tools.Vault.Data
{
    public class JWTCredentials : AuthCredentials
    {
        public JWTCredentials(AuthType authType, string jwt, string role) : base(authType)
        {
            Jwt = jwt;
            Role = role;
        }

        public string Jwt { get; private set; }
        public string Role { get; private set; }
    }
}