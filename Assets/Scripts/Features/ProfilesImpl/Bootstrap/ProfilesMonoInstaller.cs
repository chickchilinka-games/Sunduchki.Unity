using Modules.Players.Bootstrap;
using Modules.Profiles.Bootstrap;
using Modules.Profiles.Config;
using Zenject;

namespace Features.ProfilesImpl.Bootstrap
{
    public class ProfilesMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<ProfilesInstaller>();
            Container.Install<PlayersInstaller>();
            Container.Bind<IProfileApiConfigProvider>()
                .To<ProfileApiConfigProvider>()
                .AsSingle();

        }
    }
}
