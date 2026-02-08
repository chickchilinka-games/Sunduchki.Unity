using System;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.Profiles.Config;
using Modules.Profiles.Interfaces;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Modules.Profiles.Providers
{
    public sealed class BackendDisplayNameProvider : IDisplayNameProvider
    {
        private readonly IProfileApiConfigProvider _configProvider;
        private readonly ITokenProvider _tokenProvider;

        public BackendDisplayNameProvider(IProfileApiConfigProvider configProvider, ITokenProvider tokenProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _tokenProvider = tokenProvider;
        }

        public async UniTask<OperationResult> ChangeDisplayNameAsync(string displayName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return OperationResult.Failed("Display name is required.");
            }

            var token = _tokenProvider?.GetToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                return OperationResult.Failed("Authentication token is required.");
            }

            var uri = BuildUri("/api/profile/rename");
            var payload = new RenameProfileRequest { displayName = displayName };
            var json = JsonConvert.SerializeObject(payload);
            var data = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(data),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {token}");

            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            if (request.result == UnityWebRequest.Result.Success)
            {
                return OperationResult.Succeeded();
            }

            var statusCode = (HttpStatusCode)request.responseCode;
            var body = request.downloadHandler?.text;
            var message = $"Profile rename failed: {(int)statusCode} {statusCode} {request.error}";
            Debug.LogError($"[Profiles] {message}. Response: {body}");
            return OperationResult.Failed(message);
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

        private sealed class RenameProfileRequest
        {
            public string displayName;
        }
    }
}
