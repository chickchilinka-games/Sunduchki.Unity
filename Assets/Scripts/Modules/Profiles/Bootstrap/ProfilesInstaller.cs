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
#if UNITY_WEBGL && !UNITY_EDITOR
            Container.Bind<IDisplayNameProvider>().To<BackendDisplayNameProvider>().AsSingle();
#else
            Container.Bind<IDisplayNameProvider>().To<FirebaseDisplayNameProvider>().AsSingle();
#endif
            Container.Bind<ProfileService>().AsSingle();
        }
    }
}
