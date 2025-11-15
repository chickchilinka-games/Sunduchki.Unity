using Modules.Lobby.Config;
using Modules.Lobby.Interfaces;
using Modules.Lobby.Model;
using Modules.Lobby.Providers;
using Modules.Lobby.Services;
using Modules.SignalR;
using UnityEngine;
using Zenject;

namespace Modules.Lobby.Bootstrap
{
    public class LobbyInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<ILobbyApiConfigProvider>()
                .FromInstance(LoadApiConfigProvider())
                .AsSingle();

            Container.Bind<ILobbyApiClient>().To<RestLobbyApiClient>().AsSingle();
            Container.Bind<LobbyModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyStateContext>().AsSingle();
#if UNITY_WEBGL && !UNITY_EDITOR
            Container.Bind<ISignalRConnectionFactory>().To<WebGLSignalRConnectionFactory>().AsSingle();
#else
            Container.Bind<ISignalRConnectionFactory>().To<DotNetSignalRConnectionFactory>().AsSingle();
#endif
            Container.Bind<ILobbySignalRClient>().To<SignalRLobbyClient>().AsSingle();
            Container.Bind<LobbyService>().AsSingle();
        }

        private ILobbyApiConfigProvider LoadApiConfigProvider()
        {
            var asset = Resources.Load<LobbyApiConfigProviderAsset>("Lobby/LobbyApiConfig");
            if (asset != null)
            {
                return asset;
            }

            return ScriptableObject.CreateInstance<LobbyApiConfigProviderAsset>();
        }
    }
}
