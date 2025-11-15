using System;
using System.Collections.Generic;

namespace Modules.AuthenticationSystem.Data
{
    public sealed class AppleAuthCredential : WebAuthCredential
    {
        public AppleAuthCredential(
            string username,
            string displayName,
            IReadOnlyList<string> scopes,
            IReadOnlyDictionary<string, string> customParameters,
            string idToken,
            string rawNonce,
            string accessToken)
            : base(AuthType.Apple, username, displayName, scopes, customParameters)
        {
            IdToken = idToken;
            RawNonce = rawNonce;
            AccessToken = accessToken;
        }

        public string IdToken { get; }

        public string RawNonce { get; }

        public string AccessToken { get; }
    }
}
