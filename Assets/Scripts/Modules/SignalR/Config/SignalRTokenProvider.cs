using Modules.AuthenticationSystem.Services;

namespace Modules.SignalR.Config
{
    public sealed class SignalRTokenProvider : ITokenProvider
    {
        private readonly AuthenticationService _authenticationService;

        public SignalRTokenProvider(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public string GetToken()
        {
            return _authenticationService.UserContextStream.CurrentValue?.JwtToken ?? string.Empty;
        }
    }
}
