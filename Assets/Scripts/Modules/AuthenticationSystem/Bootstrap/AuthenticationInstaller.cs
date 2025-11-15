using Modules.AuthenticationSystem.Entities;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Providers;
using Modules.AuthenticationSystem.Services;
using UnityEngine;
using Zenject;

namespace Modules.AuthenticationSystem.Bootstrap
{
    public class AuthenticationInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<AuthenticationModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<AuthenticationService>().AsSingle();
            BindCredentialProviders();
        }

        private void BindCredentialProviders()
        {
            #if UNITY_IOS && !UNITY_EDITOR
            Container.BindInterfacesTo<NativeAppleCredentialProvider>().AsSingle();
            // Container.BindInterfacesTo<WebGoogleCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<AnonymousCredentialProvider>().AsSingle();
            #elif UNITY_ANDROID && !UNITY_EDITOR
            Container.BindInterfacesTo<WebAppleCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<AnonymousCredentialProvider>().AsSingle();
            #else
            Container.BindInterfacesTo<WebAppleCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<WebGoogleCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<AnonymousCredentialProvider>().AsSingle();
            #endif
        }
    }
}
