using System;
using Modules.AuthenticationSystem.Config;
using Modules.Profiles.Data;

namespace Modules.Profiles.Config
{
    public sealed class ProfileApiConfigProvider : IProfileApiConfigProvider
    {
        private readonly BaseUrlConfigAsset _baseConfig;

        public ProfileApiConfigProvider(BaseUrlConfigAsset baseConfig)
        {
            _baseConfig = baseConfig ?? throw new ArgumentNullException(nameof(baseConfig));
        }

        public ProfileApiConfig GetConfig()
        {
            var baseUri = _baseConfig.GetAccountsBaseUri();
            return new ProfileApiConfig(baseUri);
        }
    }
}
