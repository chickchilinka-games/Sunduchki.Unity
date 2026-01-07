using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.Profiles.Data;
using Modules.Profiles.Interfaces;
using R3;

namespace Modules.Profiles.Services
{
    public class ProfileService : IDisposable
    {
        private readonly Subject<string> _displayNameChanged = new();
        private readonly IProfileApiClient _apiClient;
        private readonly IDisplayNameProvider _displayNameProvider;

        public ProfileService(IProfileApiClient apiClient, IDisplayNameProvider displayNameProvider)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _displayNameProvider = displayNameProvider ?? throw new ArgumentNullException(nameof(displayNameProvider));
        }

        public UniTask<ProfileInfo> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            return _apiClient.GetProfileAsync(userId, cancellationToken);
        }

        public Observable<string> DisplayNameChanged => _displayNameChanged;

        public async UniTask<OperationResult> ChangeDisplayNameAsync(string displayName, CancellationToken cancellationToken = default)
        {
            var result = await _displayNameProvider.ChangeDisplayNameAsync(displayName, cancellationToken);
            if (result.Success)
            {
                _displayNameChanged.OnNext(displayName);
            }

            return result;
        }

        public void Dispose()
        {
            _displayNameChanged.Dispose();
        }
    }
}
