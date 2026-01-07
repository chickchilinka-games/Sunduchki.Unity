using System;

namespace Modules.AuthenticationSystem.Data
{
    [Serializable]
    public enum AuthType
    {
        None = 0,
        Anonymous = 1,
        Google = 2,
        Apple = 3,
        Email = 4
    }
}
