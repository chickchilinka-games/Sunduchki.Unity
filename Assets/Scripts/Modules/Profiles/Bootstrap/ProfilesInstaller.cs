using Modules.Profiles.Config;
using Modules.Profiles.Interfaces;
using Modules.Profiles.Providers;
using Modules.Profiles.Services;
using UnityEngine;
using Zenject;

namespace Modules.Profiles.Bootstrap
{
    public class ProfilesInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IProfileApiClient>().To<RestProfileApiClient>().AsSingle();
            Container.Bind<ITokenProvider>().To<ProfileTokenProvider>().AsSingle().IfNotBound();
            Container.Bind<IDisplayNameProvider>().To<FirebaseDisplayNameProvider>().AsSingle();
            Container.Bind<ProfileService>().AsSingle();
        }
    }
}
