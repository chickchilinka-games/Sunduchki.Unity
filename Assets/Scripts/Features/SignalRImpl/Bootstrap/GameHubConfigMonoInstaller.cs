using Modules.SignalR.Config;
using Zenject;

namespace Features.SignalRImpl.Bootstrap
{
    public class GameHubConfigMonoInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IGameHubConfigProvider>()
                .To<GameHubConfigProvider>()
                .AsSingle()
                .IfNotBound();
        }
    }
}
