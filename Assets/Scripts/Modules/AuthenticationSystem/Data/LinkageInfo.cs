using System;

namespace Modules.AuthenticationSystem.Data
{
    [Serializable]
    public sealed class LinkageInfo
    {
        internal LinkageInfo(AuthType authType, string username, string email)
        {
            AuthType = authType;
            Username = username;
            Email = email;
        }

        public AuthType AuthType { get; }

        public string Username { get; }

        public string Email { get; }
    }
}
