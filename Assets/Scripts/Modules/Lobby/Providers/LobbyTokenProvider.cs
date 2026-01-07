using Modules.AuthenticationSystem.Services;
using Modules.Lobby.Interfaces;

namespace Modules.Lobby.Providers
{
    public sealed class LobbyTokenProvider : ITokenProvider
    {
        private readonly AuthenticationService _authenticationService;

        public LobbyTokenProvider(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public string GetToken()
        {
            return _authenticationService.UserContextStream.CurrentValue?.JwtToken ?? string.Empty;
        }
    }
}
