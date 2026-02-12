using Cysharp.Threading.Tasks;
using Features.UI.Components;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Zenject;

namespace Features.AppLifecycle.States.Game.View
{
    public class OpenGameMenuButton : AbstractButton
    {
        private WindowSystem _windowSystem;

        [Inject]
        public void Construct(WindowSystem windowSystem)
        {
            _windowSystem = windowSystem;
        }

        protected override void OnClick()
        {
            _windowSystem.ShowWindowAsync<GameMenuContent>(nameof(BlockerWindowTemplate)).Forget();
        }
    }
}
