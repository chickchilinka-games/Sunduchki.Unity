using Cysharp.Threading.Tasks;
using Features.UI.Components;
using Features.WindowSystemImpl.Templates;
using ICVR.Window;
using Zenject;

namespace Features.AuthenticationSystemImpl.Views
{
    public class ShowLinkAccountWindowButton: AbstractButton
    {
        private WindowSystem _windowSystem;
        [Inject]
        public void Construct(WindowSystem windowSystem)
        {
            _windowSystem = windowSystem;
        }
        protected override void OnClick()
        {
            _windowSystem.ShowWindowAsync<ManageAccountsWindowContent>(nameof(BlockerWindowTemplate)).Forget();
        }
    }
}
