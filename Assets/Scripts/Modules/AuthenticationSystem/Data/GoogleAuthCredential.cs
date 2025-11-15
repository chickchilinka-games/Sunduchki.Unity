using System;
using System.Collections.Generic;

namespace Modules.AuthenticationSystem.Data
{
    public sealed class GoogleAuthCredential : WebAuthCredential
    {
        public GoogleAuthCredential(
            string username,
            string displayName,
            IReadOnlyList<string> scopes,
            IReadOnlyDictionary<string, string> customParameters,
            string idToken,
            string accessToken)
            : base(AuthType.Google, username, displayName, scopes, customParameters)
        {
            IdToken = idToken;
            AccessToken = accessToken;
        }

        public string IdToken { get; }

        public string AccessToken { get; }
    }
}
