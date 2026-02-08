using Modules.Lobby.Bootstrap;
using Modules.Lobby.Config;
using Zenject;

namespace Features.LobbyImpl.Bootstrap
{
    public class LobbyMonoInstaller: MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Install<LobbyInstaller>();
            Container.Bind<ILobbyApiConfigProvider>()
                .To<LobbyApiConfigProvider>()
                .AsSingle();

        }
    }
}
