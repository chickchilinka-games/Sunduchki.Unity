using Modules.PlayerHand.Interfaces;
using Modules.PlayerHand.Model;
using Modules.PlayerHand.Rules;
using Modules.PlayerHand.Services;
using Modules.SignalR.Config;
using UnityEngine;
using Zenject;

namespace Modules.PlayerHand.Bootstrap
{
    public class PlayerHandInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<PlayerHandModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerHandService>().AsSingle();
            Container.BindInterfacesTo<PlayerHandSignalRelay>().AsSingle();
            Container.Bind<IPlayerHandSignalClient>().To<SignalRPlayerHandClient>().AsSingle();
            Container.Bind<IGameHubConfigProvider>().FromInstance(LoadHubConfig()).IfNotBound();
            Container.BindInterfacesTo<TrackPlayerHandOnLobbyConnectRule>().AsSingle();
        }

        private IGameHubConfigProvider LoadHubConfig()
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
