#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Modules.SignalR.Config;
using Zenject;

namespace Modules.SignalR
{
    public sealed class DotNetSignalRConnectionFactory : ISignalRConnectionFactory
    {
        private readonly ITokenProvider _tokenProvider;

        public DotNetSignalRConnectionFactory(ITokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public ISignalRConnection Create(Uri hubUri, string accessToken)
        {
            if (hubUri == null)
            {
                throw new ArgumentNullException(nameof(hubUri));
            }

            var resolvedToken = ResolveToken(accessToken);
            var builder = new HubConnectionBuilder()
                .WithUrl(hubUri, options =>
                {
                    if (!string.IsNullOrWhiteSpace(resolvedToken))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(resolvedToken);
                    }
                    options.Transports = HttpTransportType.LongPolling;
                })
                .WithAutomaticReconnect();

            return new DotNetSignalRConnection(builder.Build(), SynchronizationContext.Current);
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
#else
namespace Modules.SignalR
{
    public sealed class DotNetSignalRConnectionFactory : ISignalRConnectionFactory
    {
        public ISignalRConnection Create(Uri hubUri, string accessToken)
        {
            throw new PlatformNotSupportedException("DotNet SignalR factory is not available on WebGL builds.");
        }
    }
}
#endif
