using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Entities;
using Modules.AuthenticationSystem.Exceptions;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;
using R3;
using UnityEngine;
using Zenject;

namespace Modules.AuthenticationSystem.Services
{
    public class AuthenticationService
    {
        public ReadOnlyReactiveProperty<AuthStatus> AuthStatusStream => _model.AuthStatusStream;
        public ReadOnlyReactiveProperty<UserContext> UserContextStream => _model.UserContextStream;

        private readonly Dictionary<AuthType, ICredentialProvider> _providersByType;
        private readonly IIdentityAuthority _authority;
        private readonly AuthenticationModel _model;

        public AuthenticationService(
            ICredentialProvider[] providers,
            IIdentityAuthority authority,
            AuthenticationModel model)
        {
            _providersByType = providers?.ToDictionary(provider => provider.AuthType) ??
                               new Dictionary<AuthType, ICredentialProvider>();
            _authority = authority;
            _model = model;
        }

        public async UniTask<AuthResult> TryAutoSignInAsync()
        {
            var cachedContext = await _authority.GetCachedUserContextAsync();
            if (cachedContext == null)
            {
                Debug.Log("[Authentication] No cached session found.");
                return AuthResult.Failure("No cached session.");
            }

            var resolvedAuthType = DeterminePrimaryAuthType(cachedContext);
            Debug.Log($"[Authentication] Restored cached session using {resolvedAuthType}.");
            _model.StartSignIn();
            _model.CompleteSignIn(cachedContext);
            return AuthResult.Succeeded(cachedContext);
        }

        public async UniTask<AuthResult> SignInAsync(AuthType authType)
        {
            if (!_providersByType.TryGetValue(authType, out var provider))
            {
                return AuthResult.Failure($"No provider registered for {authType}.");
            }

            Debug.Log($"[Authentication] Starting sign-in with {authType}.");
            return await SignInInternalAsync(provider, authType);
        }

        public async UniTask<OperationResult> SignOutAsync()
        {
            if (_model.AuthStatusStream.CurrentValue != AuthStatus.SignedIn)
            {
                Debug.LogWarning("[Authentication] Tried to sign-out without signing in.");
                return OperationResult.Failed("Not signed in.");
            }

            Debug.Log("[Authentication] Signing out current user.");

            try
            {
                await _authority.SignOutAsync();
                _model.SignOut();
                return OperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Authentication] Failed to sign out: {ex.Message}");
                return OperationResult.Failed(ex.Message);
            }
        }

        public async UniTask<UserContext> RefreshUserContextAsync(CancellationToken cancellationToken = default)
        {
            if (_model.UserContextStream.CurrentValue == null)
            {
                return null;
            }

            try
            {
                var refreshed = await _authority.RefreshAsync(cancellationToken);
                if (refreshed != null)
                {
                    _model.CompleteSignIn(refreshed);
                }

                return refreshed;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Authentication] Failed to refresh user context: {ex.Message}");
                return null;
            }
        }

        public async UniTask<LinkageResult> LinkAsync(AuthType authType)
        {
            if (!_providersByType.TryGetValue(authType, out var provider))
            {
                return LinkageResult.Failed(null, $"No provider registered for {authType}.");
            }

            if (_model.UserContextStream.CurrentValue == null)
            {
                return LinkageResult.Failed(null, "Cannot link provider without an active user session.");
            }

            Debug.Log($"[Authentication] Linking provider {authType}.");

            try
            {
                var credential = await provider.AcquireCredentialAsync();
                credential = credential ?? throw new InvalidOperationException("Credential acquisition returned null.");

                var linkageInfo = await _authority.LinkAsync(credential);

                if (linkageInfo == null)
                {
                    return LinkageResult.Failed(null, "Identity authority returned null linkage result.");
                }

                _model.Link(linkageInfo);

                return LinkageResult.Succeeded(linkageInfo);
            }
            catch (UserCancelledException ex)
            {
                Debug.Log($"[Authentication] User cancelled linking {authType}: {ex.Message}");
                return LinkageResult.Failed(null, AuthenticationConstants.ErrorCodes.UserCancelled);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Authentication] Failed to link provider {authType}: {ex.Message}");
                return LinkageResult.Failed(null, ex.Message);
            }
        }

        public async UniTask<OperationResult> UnlinkAsync(AuthType authType)
        {
            Debug.Log($"[Authentication] Unlinking provider {authType}.");

            try
            {
                if (_model.UserContextStream.CurrentValue == null)
                {
                    Debug.LogWarning("[Authentication] No active user session to unlink from.");
                    return OperationResult.Failed("Not authenticated");
                }

                await _authority.UnlinkAsync(authType);

                var refreshedContext = await _authority.GetCachedUserContextAsync();
                if (refreshedContext != null)
                {
                    _model.CompleteSignIn(refreshedContext);
                }
                else
                {
                    var currentUser = _model.UserContextStream.CurrentValue;
                    if (currentUser != null && currentUser.TryGetLinkageInfo(authType, out var linkage))
                    {
                        _model.Unlink(linkage);
                    }
                }
                return OperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Authentication] Failed to unlink provider {authType}: {ex.Message}");
                return OperationResult.Failed(ex.Message);
            }
        }

        public async UniTask<OperationResult> DeleteAccountAsync()
        {
            if (_model.AuthStatusStream.CurrentValue != AuthStatus.SignedIn)
                return OperationResult.Failed("Not signed in.");

            var userContext = _model.UserContextStream.CurrentValue;
            if (userContext == null)
                return OperationResult.Failed("No user context.");

            var authType = DeterminePrimaryAuthType(userContext);
            if (authType == AuthType.None)
                return OperationResult.Failed("No primary auth type found.");

            if (!_providersByType.TryGetValue(authType, out var provider))
                return OperationResult.Failed($"No {authType} provider.");

            try
            {
                var credential = await provider.AcquireCredentialAsync();
                await _authority.DeleteAsync(credential);

                _model.SignOut();
                return OperationResult.Succeeded();
            }
            catch (UserCancelledException ex)
            {
                Debug.Log($"[Authentication] User cancelled account deletion authorization: {ex.Message}");
                return OperationResult.Failed(AuthenticationConstants.ErrorCodes.UserCancelled);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Authentication] Failed to delete account: {ex.Message}");
                return OperationResult.Failed($"Failed to delete account: {ex.Message}");
            }
        }

        public AuthType[] GetAvailableAuthTypes()
        {
            return _providersByType.Keys.ToArray();
        }

        public bool IsLinked(AuthType authType)
        {
            var userContext = _model.UserContextStream.CurrentValue;
            return userContext != null && userContext.TryGetLinkageInfo(authType, out _);
        }

        private async UniTask<AuthResult> SignInInternalAsync(ICredentialProvider provider, AuthType authType)
        {
            _model.StartSignIn();

            try
            {
                var credential = await provider.AcquireCredentialAsync();
                credential = credential ?? throw new InvalidOperationException("Credential acquisition returned null.");

                var userContext = await _authority.SignInAsync(credential);
                if (userContext == null)
                {
                    throw new InvalidOperationException("Identity authority returned null user context.");
                }

                _model.CompleteSignIn(userContext);

                return AuthResult.Succeeded(userContext);
            }
            catch (UserCancelledException ex)
            {
                Debug.Log($"[Authentication] User cancelled sign-in with {authType}: {ex.Message}");
                _model.FailSignIn();

                return AuthResult.Failure(AuthenticationConstants.ErrorCodes.UserCancelled);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Authentication] Sign-in failed for {authType}: {ex.Message}");
                _model.FailSignIn();

                return AuthResult.Failure(ex.Message);
            }
        }

        private AuthType DeterminePrimaryAuthType(UserContext userContext)
        {
            if (userContext == null)
            {
                return AuthType.None;
            }

            var nonAnonymous = userContext.LinkedProviders
                .FirstOrDefault(lp =>
                    lp.AuthType != AuthType.Anonymous && lp.AuthType != AuthType.None &&
                    _providersByType.ContainsKey(lp.AuthType));
            if (nonAnonymous != null)
            {
                return nonAnonymous.AuthType;
            }

            if (userContext.LinkedProviders.Any(lp => lp.AuthType == AuthType.Anonymous))
            {
                return AuthType.Anonymous;
            }

            return AuthType.None;
        }
    }
}
