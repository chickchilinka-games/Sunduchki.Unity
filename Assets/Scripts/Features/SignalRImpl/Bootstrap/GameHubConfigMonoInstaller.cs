using Modules.SignalR.Config;
using UnityEngine;
using Zenject;

namespace Features.SignalRImpl.Bootstrap
{
    public class GameHubConfigMonoInstaller : MonoInstaller
    {
        [SerializeField]
        private GameHubConfigProviderAsset _gameHubConfigProviderAsset;
        public override void InstallBindings()
        {
            Container.Bind<IGameHubConfigProvider>()
                .FromMethod(context => new GameHubConfigProvider(_gameHubConfigProviderAsset, context.Container.Resolve<ITokenProvider>()))
                .AsSingle()
                .IfNotBound();
        }
    }
}
