using Modules.BonusSystem.Config;
using Modules.BonusSystem.Services;
using UnityEngine;
using Zenject;

namespace Modules.BonusSystem.Bootstrap
{
    public class BonusSystemInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<BonusActionService>().AsSingle();
            Container.BindInterfacesTo<SignalRBonusActionClient>().AsSingle();
            Container.Bind<IBonusCardRulesProvider>().FromInstance(LoadRulesProvider()).AsSingle();
            Container.BindInterfacesTo<BonusCardInfoConfigProvider>().AsSingle();
        }

        private IBonusCardRulesProvider LoadRulesProvider()
        {
            var asset = Resources.Load<BonusCardRulesConfigAsset>("BonusSystem/BonusCardRules");
            if (asset != null)
            {
                return asset;
            }

            var fallback = ScriptableObject.CreateInstance<BonusCardRulesConfigAsset>();
            fallback.SetDefaults(
                new[]
                {
                    "AskTwice",
                    "StealOneRandom",
                    "PeekNext3Deck",
                    "StealExtraOnSuccess",
                    "SilentAsk",
                    "DrawFromDeck"
                },
                new[]
                {
                    "LieOnGuess",
                    "GiveOneOnGuess",
                    "BlockGive"
                });
            return fallback;
        }
    }
}
