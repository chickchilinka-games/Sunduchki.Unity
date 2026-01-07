using Modules.AuthenticationSystem.Services;
using Modules.Profiles.Interfaces;

namespace Modules.Profiles.Providers
{
    public sealed class ProfileTokenProvider : ITokenProvider
    {
        private readonly AuthenticationService _authenticationService;

        public ProfileTokenProvider(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public string GetToken()
        {
            return _authenticationService.UserContextStream.CurrentValue?.JwtToken ?? string.Empty;
        }
    }
}
