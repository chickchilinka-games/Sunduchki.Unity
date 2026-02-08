using System;
using Modules.SignalR.Config;
using Zenject;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
namespace Modules.SignalR
{
    public sealed class WebGLSignalRConnectionFactory : ISignalRConnectionFactory
    {
        private ITokenProvider _tokenProvider;

        [Inject]
        public void Construct([InjectOptional]ITokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public ISignalRConnection Create(Uri hubUri, string accessToken)
        {
            if (hubUri == null)
            {
                throw new ArgumentNullException(nameof(hubUri));
            }

            Debug.Log($"[SignalR] Creating WebGL connection to {hubUri}");
            var host = WebGLSignalRBridgeHost.Instance;
            var resolvedToken = ResolveToken(accessToken);
            var connectionId = host.CreateConnection(hubUri.ToString(), resolvedToken);
            if (connectionId < 0)
            {
                throw new InvalidOperationException("Failed to create WebGL SignalR connection. Make sure the JS SignalR client is loaded on the page.");
            }

            var connection = new WebGLSignalRConnection(host, connectionId);
            host.Attach(connectionId, connection);
            return connection;
        }

        private string ResolveToken(string fallback)
        {
            var token = _tokenProvider?.GetToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            return fallback ?? string.Empty;
        }
    }
}
#endif
