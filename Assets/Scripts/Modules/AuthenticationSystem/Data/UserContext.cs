using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.AuthenticationSystem.Data
{
    [Serializable]
    public sealed class UserContext
    {
        public UserContext(string userId, string username, string role, string jwtToken, List<LinkageInfo> linkedProviders)
        {
            UserId = userId;
            Username = username;
            Role = role;
            JwtToken = jwtToken;
            LinkedProviders = linkedProviders ??
                              new List<LinkageInfo>(); 
        }

        public string UserId { get; }

        public string Username { get; }

        public string Role { get; }

        public string JwtToken { get; }

        public IReadOnlyList<LinkageInfo> LinkedProviders { get; }
        
        public bool TryGetLinkageInfo(AuthType authType, out LinkageInfo linkageInfo)
        {
            linkageInfo = LinkedProviders.FirstOrDefault(linkageInfo => linkageInfo.AuthType == authType);
            return linkageInfo != null;
        }
    }
}
