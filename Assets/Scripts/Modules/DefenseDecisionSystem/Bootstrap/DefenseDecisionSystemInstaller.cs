using Modules.DefenseDecisionSystem.Interfaces;
using Modules.DefenseDecisionSystem.Services;
using Modules.DefenseDecisionSystem.Rules;
using Modules.DefenseDecisionSystem.Services;
using Modules.SignalR.Config;
using UnityEngine;
using Zenject;

namespace Modules.DefenseDecisionSystem.Bootstrap
{
    public sealed class DefenseDecisionSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IDefenseDecisionClient>().To<SignalRDefenseDecisionClient>().AsSingle();
            Container.Bind<DefenseDecisionService>().AsSingle();
            Container.Bind<IDefenseDecisionPromptWriter>()
                .FromResolveGetter<DefenseDecisionService>(service => service)
                .AsSingle();
            Container.BindInterfacesTo<TrackDefenseDecisionOnLobbyConnectRule>().AsSingle();
            Container.Bind<IGameHubConfigProvider>().FromInstance(LoadConfig()).IfNotBound();
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
