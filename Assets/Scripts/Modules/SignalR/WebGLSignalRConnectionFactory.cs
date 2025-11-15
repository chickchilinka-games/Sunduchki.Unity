using System;

#if UNITY_WEBGL && !UNITY_EDITOR
namespace Modules.SignalR
{
    public sealed class WebGLSignalRConnectionFactory : ISignalRConnectionFactory
    {
        public ISignalRConnection Create(Uri hubUri, string accessToken)
        {
            if (hubUri == null)
            {
                throw new ArgumentNullException(nameof(hubUri));
            }

            var host = WebGLSignalRBridgeHost.Instance;
            var connectionId = host.CreateConnection(hubUri.ToString(), accessToken);
            if (connectionId < 0)
            {
                throw new InvalidOperationException("Failed to create WebGL SignalR connection. Make sure the JS SignalR client is loaded on the page.");
            }

            var connection = new WebGLSignalRConnection(host, connectionId);
            host.Attach(connectionId, connection);
            return connection;
        }
    }
}
#else
namespace Modules.SignalR
{
    public sealed class WebGLSignalRConnectionFactory : ISignalRConnectionFactory
    {
        public ISignalRConnection Create(Uri hubUri, string accessToken)
        {
            throw new PlatformNotSupportedException("WebGL SignalR factory is available only in WebGL builds.");
        }
    }
}
#endif
