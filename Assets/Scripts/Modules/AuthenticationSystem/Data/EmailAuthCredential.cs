using System.Collections.Generic;

namespace Modules.AuthenticationSystem.Data
{
    public class EmailAuthCredential : WebAuthCredential
    {
        public EmailAuthCredential(string username, string displayName, string password, IReadOnlyList<string> scopes,
            IReadOnlyDictionary<string, string> customParameters) : base(AuthType.Email, username, displayName, scopes,
            customParameters)
        {
            Password = password;
        }

        public string Password { get; }
    }
}
