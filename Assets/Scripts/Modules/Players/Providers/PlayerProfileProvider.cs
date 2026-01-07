using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Players.Data;
using Modules.Players.Interfaces;
using Modules.Profiles.Services;

namespace Modules.Players.Providers
{
    public class PlayerProfileProvider : IPlayerProfileProvider
    {
        private readonly ProfileService _profileService;

        public PlayerProfileProvider(ProfileService profileService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        }

        public async UniTask<PlayerProfile?> GetProfileAsync(string playerId, CancellationToken cancellationToken = default)
        {
            var profile = await _profileService.GetProfileAsync(playerId, cancellationToken);
            return new PlayerProfile(profile.DisplayName, profile.PhotoUrl);
        }
    }
}
