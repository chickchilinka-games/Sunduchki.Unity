using Features.AuthenticationSystemImpl.Providers;
using Modules.AuthenticationSystem.Adapters;
using Modules.AuthenticationSystem.Bootstrap;
using Modules.AuthenticationSystem.Providers;
using Zenject;

namespace Features.AuthenticationSystemImpl.Bootstrap
{
    public class AuthenticationMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<AuthenticationInstaller>();
            BindCredentialProviders();
            BindIdentityAuthority();
        }
        
        private void BindCredentialProviders()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Container.BindInterfacesTo<NativeAppleCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<AnonymousCredentialProvider>().AsSingle();
#elif UNITY_ANDROID && !UNITY_EDITOR
            Container.BindInterfacesTo<WebAppleCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<AnonymousCredentialProvider>().AsSingle();
#else
            Container.BindInterfacesTo<WebAppleCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<AnonymousCredentialProvider>().AsSingle();
            Container.BindInterfacesTo<EditorEmailCredentialProvider>().AsSingle();
#endif
            
        }

        private void BindIdentityAuthority()
        {
            Container.BindInterfacesTo<FirebaseIdentityAuthority>().AsSingle();
            Container.BindInterfacesTo<FirebaseAnonymousAuthAdapter>().AsSingle();
            Container.BindInterfacesTo<FirebaseAppleAuthAdapter>().AsSingle();
            Container.BindInterfacesTo<FirebaseGoogleAuthAdapter>().AsSingle();
            Container.BindInterfacesTo<FirebaseEmailAuthAdapter>().AsSingle();
        }
    }
}