using System;

namespace Modules.AuthenticationSystem.Config
{
    public struct AuthApiConfig
    {
        public AuthApiConfig(Uri baseAddress)
        {
            BaseAddress = baseAddress;
        }

        public Uri BaseAddress { get; }
    }
}
