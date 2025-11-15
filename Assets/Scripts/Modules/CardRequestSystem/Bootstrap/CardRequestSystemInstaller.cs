using Modules.CardRequestSystem.Interfaces;
using Modules.CardRequestSystem.Model;
using Modules.CardRequestSystem.Rules;
using Modules.CardRequestSystem.Services;
using Modules.SignalR.Config;
using UnityEngine;
using Zenject;

namespace Modules.CardRequestSystem.Bootstrap
{
    public class CardRequestSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<CardRequestModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<CardRequestService>().AsSingle();
            Container.BindInterfacesTo<CardRequestSignalRelay>().AsSingle();
            Container.Bind<ICardRequestSignalClient>().To<SignalRCardRequestClient>().AsSingle();
            Container.Bind<IGameHubConfigProvider>().FromInstance(LoadConfig()).IfNotBound();
            Container.BindInterfacesTo<TrackCardRequestsOnLobbyConnectRule>().AsSingle();
        }

        private IGameHubConfigProvider LoadConfig()
        {
            var asset = Resources.Load<GameHubConfigProviderAsset>("SignalR/GameHubConfig");
            if (asset != null)
            {
                return asset;
            }

            return ScriptableObject.CreateInstance<GameHubConfigProviderAsset>();
        }
    }
}
