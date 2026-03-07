using Modules.Lobby.Config;
using Modules.Lobby.Interfaces;
using Modules.Lobby.Model;
using Modules.Lobby.Providers;
using Modules.Lobby.Services;
using Modules.SignalR;
using Modules.SignalR.Config;
using UnityEngine;
using Zenject;
using ITokenProvider = Modules.Lobby.Interfaces.ITokenProvider;

namespace Modules.Lobby.Bootstrap
{
    public class LobbyInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<ILobbyApiClient>().To<RestLobbyApiClient>().AsSingle();
            Container.Bind<ITokenProvider>().To<LobbyTokenProvider>().AsSingle().IfNotBound();
            Container.Bind<LobbyModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyStateContext>().AsSingle();
#if UNITY_WEBGL && !UNITY_EDITOR
            Container.Bind<ISignalRConnectionFactory>().To<WebGLSignalRConnectionFactory>().AsSingle();
#else
            Container.Bind<ISignalRConnectionFactory>().To<DotNetSignalRConnectionFactory>().AsSingle();
#endif
            Container.BindInterfacesAndSelfTo<SharedGameHubConnection>().AsSingle();
            Container.Bind<ILobbySignalRClient>().To<SignalRLobbyClient>().AsSingle();
            Container.Bind<SignalR.Config.ITokenProvider>().To<SignalRTokenProvider>().AsSingle();
            Container.Bind<LobbyService>().AsSingle();
        }
    }
}
