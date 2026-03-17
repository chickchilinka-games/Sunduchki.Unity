using System.Collections.Generic;
using System.Threading;
using Chickchilinka.Tools.Vault.Data;
using Chickchilinka.Tools.Vault.Utils;
using UnityEngine;

namespace Chickchilinka.Tools.Vault.Entities
{
    internal class JwtConnectionProcess : BaseConnectionProcess
    {
        public override AuthType AuthType => AuthType.JWT;

        protected override string GetToken(string credentials, CancellationToken cancellationToken)
        {
            var jwtCredentials = SerializationUtils.Deserialize<JWTCredentials>(credentials);

            if (jwtCredentials == null
                || string.IsNullOrEmpty(jwtCredentials.Jwt)
                || string.IsNullOrEmpty(jwtCredentials.Role))
            {
                Debug.LogError("[Vault] LDAP credentials isn't correct!");

                return string.Empty;
            }

            var parameters = new Dictionary<string, string>
                { { "jwt", jwtCredentials.Jwt }, { "role", jwtCredentials.Role } };
            var response = Post(LoginUrl, parameters, cancellationToken);
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