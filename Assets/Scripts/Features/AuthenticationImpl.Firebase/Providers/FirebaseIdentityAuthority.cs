using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;
using UnityEngine;

namespace Modules.AuthenticationSystem.Providers
{
    public sealed class FirebaseIdentityAuthority : IIdentityAuthority
    {
        private readonly FirebaseAuth _auth;
        private readonly Dictionary<AuthType, IFirebaseAuthAdapter> _adapters;

        public FirebaseIdentityAuthority(IFirebaseAuthAdapter[] adapters)
        {
            _auth = FirebaseAuth.DefaultInstance;
            _adapters = adapters?.ToDictionary(a => a.AuthType)
                        ?? new Dictionary<AuthType, IFirebaseAuthAdapter>();
        }

        public UniTask<UserContext> GetCachedUserContextAsync()
        {
            var currentUser = _auth.CurrentUser;
            if (currentUser == null)
            {
                return UniTask.FromResult<UserContext>(null);
            }

            return BuildUserContext(currentUser);
        }

        public async UniTask<UserContext> SignInAsync(AuthCredential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));
            
            if (!_adapters.TryGetValue(credential.AuthType, out var adapter))
                throw new KeyNotFoundException($"No Firebase adapter registered for {credential.AuthType}");
            
            try
            {
                var user = await adapter.SignInAsync(credential, _auth);
                return await BuildUserContext(user);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[FirebaseIdentityAuthority] SignIn failed for {credential.AuthType}: {ex.Message}");
                throw FirebaseExceptionUtils.HandleAuthException(ex);
            }
        }

        public async UniTask<LinkageInfo> LinkAsync(AuthCredential credential)
        {
            if (credential == null)
                throw new InvalidOperationException("Credentials cannot be null.");

            CheckCurrentSession();

            var adapter = GetAuthAdapter(credential.AuthType);

            try
            {
                var linkedUser = await adapter.LinkAsync(credential, _auth.CurrentUser, _auth);
                var providerId = adapter.ProviderId;

                var linkageInfo = FindLinkageInfo(linkedUser, providerId);
                if (linkageInfo == null)
                {
                    credential.TryGet(AuthCredentialBuilder.ParameterKeys.Email, out string credentialEmail);
                    var resolvedName = ResolveUsername(
                        credential.AuthType,
                        credential.Username,
                        credentialEmail,
                        credential.AuthType.ToString());

                    linkageInfo = new LinkageInfo(credential.AuthType, resolvedName, credentialEmail);
                }

                return linkageInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[FirebaseIdentityAuthority] Link failed for {credential.AuthType}: {ex.Message}");
                throw FirebaseExceptionUtils.HandleAuthException(ex);
            }
        }
        
        public async UniTask DeleteAsync(AuthCredential credential)
        {
            var user = _auth.CurrentUser;
            if (user == null)
                throw new InvalidOperationException("No active user.");

            try
            {
                if (!user.IsAnonymous)
                {
                    var adapter = GetAuthAdapter(credential.AuthType);
                    await adapter.ReauthenticateAsync(credential, user, _auth);
                }

                await user.DeleteAsync();
            }catch (Exception ex)
            {
                Debug.LogError(
                    $"[FirebaseIdentityAuthority] Delete failed for {credential.AuthType}: {ex.Message}");
                throw FirebaseExceptionUtils.HandleAuthException(ex);
            }
        }

        public async UniTask UnlinkAsync(AuthType authType)
        {
            CheckCurrentSession();

            var adapter = GetAuthAdapter(authType);

            var providerId = adapter.ProviderId;
            if (string.IsNullOrEmpty(providerId))
                throw new InvalidOperationException($"ProviderId is empty for {authType} authentication.");

            try
            {
                await _auth.CurrentUser.UnlinkAsync(providerId);
            }catch (Exception ex)
            {
                Debug.LogError(
                    $"[FirebaseIdentityAuthority] Unlink failed for {authType}: {ex.Message}");
                throw FirebaseExceptionUtils.HandleAuthException(ex);
            }
        }
        
        public UniTask SignOutAsync()
        {
            try
            {
                _auth.SignOut();
                return UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"[FirebaseIdentityAuthority] Sign out failed: {ex.Message}");
                throw FirebaseExceptionUtils.HandleAuthException(ex);
            }
        }

        public async UniTask<UserContext> RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var user = _auth.CurrentUser;
            if (user == null)
            {
                return null;
            }

            try
            {
                await user.ReloadAsync();
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                return await BuildUserContext(user);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseIdentityAuthority] Refresh failed: {ex.Message}");
                throw FirebaseExceptionUtils.HandleAuthException(ex);
            }
        }

        private void CheckCurrentSession()
        {
            if (_auth.CurrentUser == null)
                throw new InvalidOperationException("No active user session.");
        }

        private IFirebaseAuthAdapter GetAuthAdapter(AuthType authType)
        {
            if (!_adapters.TryGetValue(authType, out var adapter))
                throw new NotSupportedException($"Could not found firebase adapter for {authType}.");

            return adapter;
        }

        private async UniTask<UserContext> BuildUserContext(FirebaseUser user)
        {
            if (user == null)
                return null;

            var linked = user.ProviderData?
                .Select(CreateLinkageInfo)
                .Where(i => i != null)
                .ToList() ?? new List<LinkageInfo>();

            if (user.IsAnonymous && linked.TrueForAll(x => x.AuthType != AuthType.Anonymous))
            {
                var anonymousName = ResolveUsername(AuthType.Anonymous, user.DisplayName, user.Email, user.UserId);
                linked.Add(new LinkageInfo(AuthType.Anonymous, anonymousName, user.Email));
            }

            string jwtToken;
            try
            {
                jwtToken = await user.TokenAsync(false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseIdentityAuthority] Failed to retrieve token (offline?): {ex.Message}");
                return new UserContext(
                    user.UserId,
                    "Anonymous",
                    "user",
                    null,
                    linked);
            }

            var role = FirebaseJwtHelper.ExtractRole(jwtToken) ?? "user";
            return new UserContext(
                user.UserId,
                ResolveUsername(user.DisplayName, user.Email, user.UserId),
                role,
                jwtToken,
                linked);
        }

        private LinkageInfo FindLinkageInfo(FirebaseUser user, string providerId)
        {
            var info = user.ProviderData.FirstOrDefault(p =>
                string.Equals(p.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
            return CreateLinkageInfo(info);
        }

        private LinkageInfo CreateLinkageInfo(IUserInfo info)
        {
            if (info == null)
                return null;

            var authType = ProviderIdToAuthType(info.ProviderId);
            if (authType == AuthType.None) return null;

            var username = ResolveUsername(authType, info.DisplayName, info.Email, info.UserId);
            return new LinkageInfo(authType, username, info.Email);
        }

        private AuthType ProviderIdToAuthType(string providerId)
        {
            if (string.IsNullOrEmpty(providerId)) return AuthType.None;

            var adapter = _adapters.Values.FirstOrDefault(a => a.ProviderId == providerId);
            if (adapter == null)
                throw new KeyNotFoundException($"No Firebase adapter registered for {providerId}");
            return adapter.AuthType;
        }
        
        private static string ResolveUsername(string displayName, string email, string fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }

            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }

            return string.Empty;
        }

        private static string ResolveUsername(AuthType authType, string displayName, string email, string fallback = null)
        {
            var userName = ResolveUsername(displayName, email, fallback);
            if (!string.IsNullOrWhiteSpace(userName))
            {
                return userName;
            }

            return authType.ToString();
        }
    }
}
