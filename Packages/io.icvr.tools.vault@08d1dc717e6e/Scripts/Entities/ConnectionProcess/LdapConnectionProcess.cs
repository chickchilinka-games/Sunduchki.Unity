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
using System.Threading;
using System.Threading.Tasks;
using ICVR.Tools.Vault.Data;
using ICVR.Tools.Vault.Utils;
using UnityEngine;

namespace ICVR.Tools.Vault.Entities
{
    internal class LdapConnectionProcess : BaseConnectionProcess
    {
        private const string PasswordKey = "password";
        
        public override AuthType AuthType => AuthType.LDAP;

        protected override string GetToken(string credentials, CancellationToken cancellationToken)
        {
            var ldapCredentials = SerializationUtils.Deserialize<LDAPCredentials>(credentials);

            if (ldapCredentials == null
                || string.IsNullOrEmpty(ldapCredentials.Nickname)
                || string.IsNullOrEmpty(ldapCredentials.Password))
            {
                Debug.LogError($"[Vault] LDAP credentials isn't correct!");

                return string.Empty;
            }
            
            var parameters = new Dictionary<string, string>() { { PasswordKey, ldapCredentials.Password } };
            var response = Post(LoginUrl + '/' + ldapCredentials.Nickname, parameters, cancellationToken);
            var responseData = SerializationUtils.Deserialize<LoginResponseData>(response);

            if (responseData == null || string.IsNullOrEmpty(responseData?.auth?.client_token))
            {
                Debug.LogError($"Vault is not accessed! Try again later");

                return string.Empty;
            }
            
            return responseData.auth.client_token;
        }
    }
}