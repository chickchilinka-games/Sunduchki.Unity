using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Profiles.Config;
using Modules.Profiles.Data;
using Modules.Profiles.Interfaces;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Modules.Profiles.Providers
{
    public class RestProfileApiClient : IProfileApiClient
    {
        private readonly IProfileApiConfigProvider _configProvider;
        private readonly ITokenProvider _tokenProvider;

        public RestProfileApiClient(IProfileApiConfigProvider configProvider, ITokenProvider tokenProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _tokenProvider = tokenProvider;
        }

        public async UniTask<ProfileInfo> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId is required.", nameof(userId));
            }

            var token = _tokenProvider?.GetToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Profile request requires an authentication token.");
            }

            var uri = BuildUri($"/api/profile/{UnityWebRequest.EscapeURL(userId)}");
            using var request = UnityWebRequest.Get(uri);
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            EnsureSuccess(request, "get profile");

            var payload = request.downloadHandler.text;
            var dto = JsonConvert.DeserializeObject<ProfileDto>(payload);
            if (dto == null)
            {
                throw new InvalidOperationException("Profile API returned invalid response.");
            }

            return new ProfileInfo(dto.DisplayName ?? string.Empty, dto.PhotoUrl ?? string.Empty);
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
            var message = $"Profile API failed to {context}: {(int)statusCode} {statusCode} {request.error}";
            Debug.LogError($"[Profiles] {message}. Response: {body}");
            throw new InvalidOperationException(message);
        }

        private sealed class ProfileDto
        {
            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("photoUrl")]
            public string PhotoUrl { get; set; }
        }
    }
}
