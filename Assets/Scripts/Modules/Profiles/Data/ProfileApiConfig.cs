using System;

namespace Modules.Profiles.Data
{
    public struct ProfileApiConfig
    {
        public Uri BaseAddress { get; }

        public ProfileApiConfig(Uri baseAddress)
        {
            BaseAddress = baseAddress;
        }
    }
}
