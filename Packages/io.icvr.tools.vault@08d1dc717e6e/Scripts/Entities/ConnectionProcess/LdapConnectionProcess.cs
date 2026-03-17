using System.Collections.Generic;
using System.Threading;
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
                Debug.LogError("[Vault] LDAP credentials isn't correct!");

                return string.Empty;
            }

            var parameters = new Dictionary<string, string> { { PasswordKey, ldapCredentials.Password } };
            var response = Post(LoginUrl + '/' + ldapCredentials.Nickname, parameters, cancellationToken);
            var responseData = SerializationUtils.Deserialize<LoginResponseData>(response);

            if (responseData == null || string.IsNullOrEmpty(responseData?.auth?.client_token))
            {
                Debug.LogError("Vault is not accessed! Try again later");

                return string.Empty;
            }

            return responseData.auth.client_token;
        }
    }
}