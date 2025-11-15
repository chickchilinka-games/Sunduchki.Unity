using System;
using System.Collections.Generic;

namespace Modules.AuthenticationSystem.Data
{
    public class WebAuthCredential
    {
        public WebAuthCredential(
            AuthType authType,
            string username,
            string displayName,
            IReadOnlyList<string> scopes,
            IReadOnlyDictionary<string, string> customParameters)
        {
            if (authType == AuthType.None)
            {
                throw new ArgumentException("Web credentials require a concrete auth type.", nameof(authType));
            }

            AuthType = authType;
            Username = username;
            DisplayName = displayName;
            Scopes = scopes ?? Array.Empty<string>();
            CustomParameters = customParameters ?? new Dictionary<string, string>();
        }

        public AuthType AuthType { get; }

        public string Username { get; }

        public string DisplayName { get; }

        public IReadOnlyList<string> Scopes { get; }

        public IReadOnlyDictionary<string, string> CustomParameters { get; }
    }
}
