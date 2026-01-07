using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.CardRequestSystem.Data;
using Modules.CardRequestSystem.Interfaces;

namespace Modules.CardRequestSystem.Services
{
    public class CardRequestTrackingService
    {
        private readonly ICardRequestSignalClient _signalClient;
        private readonly ICardRequestSignalHandler _signalHandler;
        private IDisposable _subscription;

        public CardRequestTrackingService(
            ICardRequestSignalClient signalClient,
            ICardRequestSignalHandler signalHandler)
        {
            _signalClient = signalClient ?? throw new ArgumentNullException(nameof(signalClient));
            _signalHandler = signalHandler ?? throw new ArgumentNullException(nameof(signalHandler));
        }

        public async UniTask StartAsync(CardRequestTrackingOptions options, CancellationToken cancellationToken = default)
        {
            await StopAsync();
            _subscription = await _signalClient.SubscribeAsync(options, _signalHandler, cancellationToken);
        }

        public async UniTask StopAsync()
        {
            _subscription?.Dispose();
            _subscription = null;
            _signalHandler.ResetState();
            await UniTask.CompletedTask;
        }
    }
}
