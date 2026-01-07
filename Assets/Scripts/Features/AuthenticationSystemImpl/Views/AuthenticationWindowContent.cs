using System.Linq;
using Cysharp.Threading.Tasks;
using Features.LoadingScreen.Services;
using ICVR.Window.Abstract;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Services;
using TMPro;
using UnityEngine;
using Zenject;

namespace Features.AuthenticationSystemImpl.Views
{
    public class AuthenticationWindowContent : AbstractContent
    {
        [SerializeField] private AuthenticationButton[] _authenticationButtons;
        [SerializeField] private TMP_Text _errorTMP;
        public override string Title => "Sign In";

        private AuthenticationService _authenticationService;
        private ILoadingScreenService _loadingScreenService;

        [Inject]
        public void Construct(AuthenticationService authenticationService, ILoadingScreenService loadingScreenService)
        {
            _authenticationService = authenticationService;
            _loadingScreenService = loadingScreenService;
        }

        private void Awake()
        {
            var availableAuthTypes = _authenticationService.GetAvailableAuthTypes();
            foreach (var button in _authenticationButtons)
            {
                button.Initialize(availableAuthTypes.Contains(button.AuthType),
                    Authenticate);
            }
        }

        private void OnEnable()
        {
            _loadingScreenService.Hide();
            _errorTMP.text = "";
        }

        private void Authenticate(AuthType authType)
        {
            _loadingScreenService.Show();

            _authenticationService.SignInAsync(authType).ContinueWith(result =>
            {
                _loadingScreenService.Hide();
                _errorTMP.enabled = !result.Success;
                _errorTMP.text = result.Error;
            });
        }
    }
}