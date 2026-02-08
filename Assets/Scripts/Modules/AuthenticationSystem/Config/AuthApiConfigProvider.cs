using System;

namespace Modules.AuthenticationSystem.Config
{
    public sealed class AuthApiConfigProvider : IAuthApiConfigProvider
    {
        private readonly BaseUrlConfigAsset _baseConfig;

        public AuthApiConfigProvider(BaseUrlConfigAsset baseConfig)
        {
            _baseConfig = baseConfig ?? throw new ArgumentNullException(nameof(baseConfig));
        }

        public AuthApiConfig GetConfig()
        {
            var baseUri = _baseConfig.GetAccountsBaseUri();
            return new AuthApiConfig(baseUri);
        }
    }
}
