using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Services;
using Modules.Profiles.Services;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Features.ProfilesImpl.View
{
    public class NicknameText : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _nameField;

        private AuthenticationService _authenticationService;
        private ProfileService _profileService;

        [Inject]
        public void Construct(AuthenticationService authenticationService, ProfileService profileService)
        {
            _authenticationService = authenticationService;
            _profileService = profileService;
        }

        private void Awake()
        {
            _authenticationService.UserContextStream
                .Subscribe(user => _nameField.text = user?.Username ?? "")
                .AddTo(this);
            _nameField.OnEndEditAsObservable()
                .Subscribe(value => _profileService.ChangeDisplayNameAsync(value).Forget())
                .AddTo(this);
        }
    }
}
