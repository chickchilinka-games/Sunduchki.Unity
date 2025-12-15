using Modules.BonusSystem.Interfaces;
using Modules.BonusSystem.Rules;
using Modules.BonusSystem.Services;
using Modules.SignalR.Config;
using UnityEngine;
using Zenject;

namespace Modules.BonusSystem.Bootstrap
{
    public class BonusSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<BonusActionService>().AsSingle();
            Container.Bind<IBonusActionClient>().To<SignalRBonusActionClient>().AsSingle();
            Container.Bind<IGameHubConfigProvider>().FromInstance(LoadConfig()).IfNotBound();
            Container.BindInterfacesTo<TrackBonusActionsOnLobbyConnectRule>().AsSingle();
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
