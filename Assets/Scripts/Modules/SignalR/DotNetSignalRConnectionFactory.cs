#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Modules.SignalR
{
    public sealed class DotNetSignalRConnectionFactory : ISignalRConnectionFactory
    {
        public ISignalRConnection Create(Uri hubUri, string accessToken)
        {
            if (hubUri == null)
            {
                throw new ArgumentNullException(nameof(hubUri));
            }

            var builder = new HubConnectionBuilder()
                .WithUrl(hubUri, options =>
                {
                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(accessToken);
                    }
                })
                .WithAutomaticReconnect();

            return new DotNetSignalRConnection(builder.Build());
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
