using Modules.DeckSystem.Interfaces;
using Modules.DeckSystem.Model;
using Modules.DeckSystem.Rules;
using Modules.DeckSystem.Services;
using Modules.SignalR.Config;
using UnityEngine;
using Zenject;

namespace Modules.DeckSystem.Bootstrap
{
    public class DeckSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<DeckModel>().AsSingle();
            Container.BindInterfacesAndSelfTo<DeckService>().AsSingle();
            Container.BindInterfacesTo<DeckSignalRelay>().AsSingle();
            Container.Bind<IDeckSignalClient>().To<SignalRDeckClient>().AsSingle();
            Container.Bind<IGameHubConfigProvider>().FromInstance(LoadConfig()).IfNotBound();
            Container.BindInterfacesTo<TrackDeckOnLobbyConnectRule>().AsSingle();
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
