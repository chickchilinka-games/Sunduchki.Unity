using System;

namespace Modules.AuthenticationSystem.Data
{
    [Serializable]
    public enum AuthStatus
    {
        None = 0,
        SignedOut = 1,
        SigningIn = 2,
        SignedIn = 3,
        SigningOut = 4
    }
}
