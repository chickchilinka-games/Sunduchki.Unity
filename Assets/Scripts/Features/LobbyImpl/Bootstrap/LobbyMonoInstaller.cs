using Modules.Lobby.Bootstrap;
using Modules.Lobby.Config;
using UnityEngine;
using Zenject;

namespace Features.LobbyImpl.Bootstrap
{
    public class LobbyMonoInstaller: MonoInstaller
    {
        [SerializeField]
        private LobbyApiConfigProviderAsset _lobbyApiConfigProviderAsset;
        public override void InstallBindings()
        {
            Container.Install<LobbyInstaller>();
            Container.Bind<ILobbyApiConfigProvider>()
                .FromInstance(_lobbyApiConfigProviderAsset)
                .AsSingle();

        }
    }
}