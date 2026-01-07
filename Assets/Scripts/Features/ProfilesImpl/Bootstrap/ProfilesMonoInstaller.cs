using Modules.Players.Bootstrap;
using Modules.Profiles.Bootstrap;
using Modules.Profiles.Config;
using UnityEngine;
using Zenject;

namespace Features.ProfilesImpl.Bootstrap
{
    public class ProfilesMonoInstaller: MonoInstaller
    {
        [SerializeField]
        private ProfileApiConfigProviderAsset _profileApiConfigProviderAsset;
        public override void InstallBindings()
        {
            Container.Install<ProfilesInstaller>();
            Container.Install<PlayersInstaller>();
            Container.Bind<IProfileApiConfigProvider>()
                .FromInstance(_profileApiConfigProviderAsset)
                .AsSingle();

        }
    }
}