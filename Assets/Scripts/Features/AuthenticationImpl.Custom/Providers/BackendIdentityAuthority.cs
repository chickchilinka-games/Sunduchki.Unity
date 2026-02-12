using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Config;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Modules.AuthenticationSystem.Providers
{
    public sealed class BackendIdentityAuthority : IIdentityAuthority
    {
        private const string CacheKey = "sunduchki.auth.backend.cache";

        private readonly IAuthApiConfigProvider _configProvider;

        public BackendIdentityAuthority(IAuthApiConfigProvider configProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        }

        public UniTask<UserContext> GetCachedUserContextAsync()
        {
            var cached = LoadCache();
            if (cached == null || string.IsNullOrWhiteSpace(cached.AccessToken))
            {
                if (TryGetTelegramInitData(out var initData) && !string.IsNullOrWhiteSpace(initData))
                {
                    return AutoSignInTelegramAsync(initData);
                }

                return UniTask.FromResult<UserContext>(null);
            }

            return UniTask.FromResult(BuildUserContext(cached.UserId, cached.Username, cached.AccessToken,
                cached.AuthType));
        }

        public async UniTask<UserContext> SignInAsync(AuthCredential credential)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            switch (credential.AuthType)
            {
                case AuthType.Telegram:
                    return await SignInTelegramAsync(credential);
                case AuthType.Anonymous:
                    return await SignInGuestAsync(credential);
                default:
                    throw new NotSupportedException($"Backend auth does not support {credential.AuthType}.");
            }
        }

        public UniTask<LinkageInfo> LinkAsync(AuthCredential credential)
        {
            throw new NotSupportedException("Backend auth does not support linking providers yet.");
        }

        public UniTask DeleteAsync(AuthCredential credential)
        {
            throw new NotSupportedException("Backend auth does not support account deletion yet.");
        }

        public UniTask UnlinkAsync(AuthType authType)
        {
            throw new NotSupportedException("Backend auth does not support unlinking providers yet.");
        }

        public UniTask SignOutAsync()
        {
            ClearCache();
            return UniTask.CompletedTask;
        }

        public async UniTask<UserContext> RefreshAsync(CancellationToken cancellationToken = default)
        {
            var cached = LoadCache();
            if (cached == null || string.IsNullOrWhiteSpace(cached.AccessToken))
            {
                return null;
            }

            try
            {
                var profile = await GetProfileAsync(cached.UserId, cached.AccessToken, cancellationToken);
                if (profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName))
                {
                    cached.Username = profile.DisplayName;
                    SaveCache(cached);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BackendIdentityAuthority] Refresh failed: {ex.Message}");
            }

            return BuildUserContext(cached.UserId, cached.Username, cached.AccessToken, cached.AuthType);
        }

        private async UniTask<UserContext> SignInGuestAsync(AuthCredential credential)
        {
            credential.TryGet(AuthCredentialBuilder.ParameterKeys.DeviceId, out string deviceId);
            credential.TryGet(AuthCredentialBuilder.ParameterKeys.DisplayName, out string displayName);

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidOperationException("Guest sign-in requires a deviceId.");
            }

            var request = new GuestSignInRequest
            {
                deviceId = deviceId,
                displayName = string.IsNullOrWhiteSpace(displayName) ? credential.Username : displayName
            };

            var response = await PostAsync<AuthResponseDto>("/api/auth/guest", request);
            var cache = new CachedAuthContext
            {
                UserId = response.playerId,
                Username = response.displayName,
                AccessToken = response.accessToken,
                AuthType = AuthType.Anonymous
            };
            SaveCache(cache);
            return BuildUserContext(cache.UserId, cache.Username, cache.AccessToken, cache.AuthType);
        }

        private async UniTask<UserContext> SignInTelegramAsync(AuthCredential credential)
        {
            credential.TryGet(AuthCredentialBuilder.ParameterKeys.InitData, out string initData);
            credential.TryGet(AuthCredentialBuilder.ParameterKeys.DisplayName, out string displayName);

            if (string.IsNullOrWhiteSpace(initData))
            {
                throw new InvalidOperationException("Telegram sign-in requires initData.");
            }

            var request = new TelegramSignInRequest
            {
                initData = initData,
                displayName = string.IsNullOrWhiteSpace(displayName) ? credential.Username : displayName
            };

            var response = await PostAsync<AuthResponseDto>("/api/auth/telegram", request);
            var cache = new CachedAuthContext
            {
                UserId = response.playerId,
                Username = response.displayName,
                AccessToken = response.accessToken,
                AuthType = AuthType.Telegram
            };
            SaveCache(cache);
            return BuildUserContext(cache.UserId, cache.Username, cache.AccessToken, cache.AuthType);
        }

        private UniTask<UserContext> AutoSignInTelegramAsync(string initData)
        {
            var credential = AuthCredentialBuilder
                .For(AuthType.Telegram)
                .WithParameter(AuthCredentialBuilder.ParameterKeys.InitData, initData)
                .Build();
            return SignInTelegramAsync(credential);
        }

        private static bool TryGetTelegramInitData(out string initData)
        {
            initData = TelegramWebAppBridge.GetInitData();
            return !string.IsNullOrWhiteSpace(initData);
        }

        private async UniTask<ProfileDto> GetProfileAsync(string userId, string token, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var uri = BuildUri($"/api/profile/{UnityWebRequest.EscapeURL(userId)}");
            using var request = UnityWebRequest.Get(uri);
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            EnsureSuccess(request, "get profile");

            var payload = request.downloadHandler.text;
            return JsonUtility.FromJson<ProfileDto>(payload);
        }

        private async UniTask<T> PostAsync<T>(string path, object payload)
        {
            var uri = BuildUri(path);
            var json = JsonUtility.ToJson(payload);
            var data = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(data),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest().ToUniTask();
            EnsureSuccess(request, path);

            var responseBody = request.downloadHandler.text;
            return JsonUtility.FromJson<T>(responseBody);
        }

        private string BuildUri(string relativePath)
        {
            var config = _configProvider.GetConfig();
            var baseAddress = config.BaseAddress ?? new Uri("http://localhost");
            var builder = new UriBuilder(baseAddress);
            var trimmedBasePath = builder.Path?.TrimEnd('/') ?? string.Empty;
            var trimmedRelative = relativePath.TrimStart('/');
            builder.Path = $"{trimmedBasePath}/{trimmedRelative}";
            builder.Query = string.Empty;
            return builder.Uri.ToString();
        }

        private static void EnsureSuccess(UnityWebRequest request, string context)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                return;
            }

            var statusCode = (HttpStatusCode)request.responseCode;
            var body = request.downloadHandler?.text;
            var message = $"Auth API failed to {context}: {(int)statusCode} {statusCode} {request.error}";
            Debug.LogError($"[BackendIdentityAuthority] {message}. Response: {body}");
            throw new InvalidOperationException(message);
        }

        private static UserContext BuildUserContext(string userId, string username, string token, AuthType authType)
        {
            var linked = new List<LinkageInfo>
            {
                new(authType, username ?? string.Empty, null)
            };

            return new UserContext(
                userId,
                username ?? string.Empty,
                "user",
                token,
                linked);
        }

        private static CachedAuthContext LoadCache()
        {
            if (!PlayerPrefs.HasKey(CacheKey))
            {
                return null;
            }

            var json = PlayerPrefs.GetString(CacheKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonUtility.FromJson<CachedAuthContext>(json);
        }

        private static void SaveCache(CachedAuthContext cache)
        {
            if (cache == null)
            {
                return;
            }

            var json = JsonUtility.ToJson(cache);
            PlayerPrefs.SetString(CacheKey, json);
            PlayerPrefs.Save();
        }

        private static void ClearCache()
        {
            if (!PlayerPrefs.HasKey(CacheKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(CacheKey);
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class GuestSignInRequest
        {
            public string deviceId;
            public string displayName;
        }

        [Serializable]
        private sealed class TelegramSignInRequest
        {
            public string initData;
            public string displayName;
        }

        [Serializable]
        private sealed class AuthResponseDto
        {
            public string accessToken;
            public string playerId;
            public string displayName;
        }

        [Serializable]
        private sealed class ProfileDto
        {
            public string displayName;
            public string photoUrl;

            public string DisplayName => displayName;
        }

        [Serializable]
        private sealed class CachedAuthContext
        {
            public string UserId;
            public string Username;
            public string AccessToken;
            public AuthType AuthType;
        }
    }
}
