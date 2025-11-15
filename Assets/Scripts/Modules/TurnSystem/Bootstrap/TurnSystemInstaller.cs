using Modules.TurnSystem.Interfaces;
using Modules.TurnSystem.Model;
using Modules.TurnSystem.Rules;
using Modules.TurnSystem.Services;
using Modules.SignalR.Config;
using UnityEngine;
using Zenject;

namespace Modules.TurnSystem.Bootstrap
{
    public class TurnSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<TurnSequenceModel>().AsSingle();
            Container.Bind<IGameHubConfigProvider>()
                .FromInstance(LoadConfigProvider())
                .IfNotBound();

            Container.Bind<ITurnSequenceService>().To<TurnSequenceService>().AsSingle();
            Container.Bind<ITurnSignalClient>().To<SignalRTurnClient>().AsSingle();

            Container.BindInterfacesTo<TrackTurnOnLobbyConnectRule>().AsSingle();
        }

        private IGameHubConfigProvider LoadConfigProvider()
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
