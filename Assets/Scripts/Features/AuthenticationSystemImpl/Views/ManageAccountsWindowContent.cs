using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.LoadingScreen.Services;
using ICVR.Window.Abstract;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Services;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Features.AuthenticationSystemImpl.Views
{
    public class ManageAccountsWindowContent : AbstractContent
    {
        [SerializeField] private Button _signOutButton;
        [SerializeField] private LinkAccountButton[] _linkAccountButtons;
        [SerializeField] private TMP_Text _errorTMP;
        [Header("User Info")]
        [SerializeField] private GameObject _userIdRoot;
        [SerializeField] private TMP_Text _userIdTMP;
        [SerializeField] private Button _copyUserIdButton;
        [SerializeField] private TMP_Text _copyTooltipTMP;
        public override string Title => "Manage Account";

        private AuthenticationService _authenticationService;
        private LoadingScreenService _loadingScreenService;
        private IDisposable _userContextSubscription;
        private string _currentUserId;
        private CancellationTokenSource _copyResetCts;
        private string _defaultCopyTooltipText;

        [Inject]
        public void Construct(AuthenticationService authenticationService, LoadingScreenService loadingScreenService)
        {
            _authenticationService = authenticationService;
            _loadingScreenService = loadingScreenService;
        }

        private void Awake()
        {
            _signOutButton.onClick.AsObservable()
                .Subscribe(_ => SignOut().Forget())
                .AddTo(this);

            if (_copyUserIdButton != null)
            {
                _copyUserIdButton.onClick.AsObservable()
                    .Subscribe(_ => CopyUserIdToClipboard())
                    .AddTo(this);
            }

            _defaultCopyTooltipText = _copyTooltipTMP != null ? _copyTooltipTMP.text : string.Empty;
        }

        private void OnEnable()
        {
            _errorTMP.text = "";
            _loadingScreenService.Hide();
            RefreshUserContext();
            _userContextSubscription = _authenticationService.UserContextStream
                .Subscribe(_ => RefreshUserContext());
        }

        private void OnDisable()
        {
            _userContextSubscription?.Dispose();
            _userContextSubscription = null;
            CancelCopyFeedback();
        }

        private void RefreshUserContext()
        {
            var userContext = _authenticationService.UserContextStream.CurrentValue;
            UpdateUserId(userContext);
            UpdateButtons(userContext);
        }

        private void OnOperationCompleted(OperationResult result)
        {
            _loadingScreenService.Hide();
            _errorTMP.enabled = !result.Success;
            _errorTMP.text = result.Error;
        }

        private void UpdateButtons(UserContext userContext)
        {
            var availableAuthTypes = _authenticationService.GetAvailableAuthTypes();
           
            if (userContext == null)
            {
                foreach (var button in _linkAccountButtons)
                {
                    button.Initialize(false, ToggleAccountLinkage, false);
                }
                return;
            }

            foreach (var button in _linkAccountButtons)
            {
                if (userContext.TryGetLinkageInfo(button.AuthType, out var linkageInfo))
                    button.Initialize(availableAuthTypes.Contains(button.AuthType), ToggleAccountLinkage, true,
                        linkageInfo);
                else
                    button.Initialize(availableAuthTypes.Contains(button.AuthType), ToggleAccountLinkage, false);
            }
        }

        private void UpdateUserId(UserContext userContext)
        {
            var hasUserId = !string.IsNullOrEmpty(userContext?.UserId);
            _currentUserId = hasUserId ? userContext.UserId : string.Empty;

            if (_userIdRoot != null)
            {
                _userIdRoot.SetActive(hasUserId);
            }

            if (_userIdTMP != null)
            {
                _userIdTMP.text = hasUserId ? userContext.UserId : string.Empty;
            }

            if (_copyUserIdButton != null)
            {
                _copyUserIdButton.interactable = hasUserId;
            }

            if (_copyTooltipTMP != null)
            {
                _copyTooltipTMP.gameObject.SetActive(hasUserId);
                if (hasUserId)
                {
                    _copyTooltipTMP.text = _defaultCopyTooltipText;
                }
            }

            CancelCopyFeedback();
        }

        private void CopyUserIdToClipboard()
        {
            if (string.IsNullOrEmpty(_currentUserId))
            {
                return;
            }

            GUIUtility.systemCopyBuffer = _currentUserId;
            if (_copyTooltipTMP != null)
            {
                _copyTooltipTMP.text = "Copied";
                ResetCopyTooltipAsync().Forget();
            }
        }

        private void CancelCopyFeedback()
        {
            if (_copyResetCts == null)
            {
                return;
            }

            _copyResetCts.Cancel();
            _copyResetCts.Dispose();
            _copyResetCts = null;
        }

        private async UniTaskVoid ResetCopyTooltipAsync()
        {
            CancelCopyFeedback();
            if (_copyTooltipTMP == null)
            {
                return;
            }

            _copyResetCts = new CancellationTokenSource();
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: _copyResetCts.Token);
                if (_copyTooltipTMP != null)
                {
                    _copyTooltipTMP.text = _defaultCopyTooltipText;
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored - another copy action happened.
            }
        }


        private async UniTask SignOut()
        {
            _loadingScreenService.Show();
            var result = await _authenticationService.SignOutAsync();
            OnOperationCompleted(result);
            if (result.Success)
                Close();
        }

        private void ToggleAccountLinkage(AuthType authType)
        {
            _loadingScreenService.Show();

            if (_authenticationService.IsLinked(authType))
                _authenticationService.UnlinkAsync(authType)
                    .ContinueWith(OnOperationCompleted);
            else
                _authenticationService.LinkAsync(authType)
                    .ContinueWith(OnOperationCompleted);
        }
    }
}
