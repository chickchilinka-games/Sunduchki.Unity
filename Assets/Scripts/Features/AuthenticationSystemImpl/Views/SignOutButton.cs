using Cysharp.Threading.Tasks;
using Features.UI.Components;
using Modules.AuthenticationSystem.Services;
using Zenject;

namespace Features.AuthenticationSystemImpl.Views
{
    public class SignOutButton: AbstractButton
    {
        private AuthenticationService _authenticationService;

        [Inject]
        public void Construct(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }
        protected override void OnClick()
        {
            _authenticationService.SignOutAsync().Forget();
        }
    }
}