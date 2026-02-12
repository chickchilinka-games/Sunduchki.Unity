using R3;
using UnityEngine;

namespace Features.UI.Components.TabButtons
{
    [DefaultExecutionOrder(-500)]
    public class TabButtonHolder : MonoBehaviour
    {
        [SerializeField]
        private TabButton[] _tabButtons;

        public ReadOnlyReactiveProperty<string> SelectedItem => _selectedItem;
        private readonly ReactiveProperty<string> _selectedItem = new();
        private readonly CompositeDisposable _buttonsSubscription = new CompositeDisposable();

        private void Awake()
        {
            foreach (var tabButton in _tabButtons)
            {
                tabButton.OnClick.Subscribe(_ => SelectTab(tabButton))
                    .AddTo(_buttonsSubscription);
            }
            
            if(_tabButtons.Length > 0)
                SelectTab(_tabButtons[0], true);
        }

        private void SelectTab(TabButton tabButton, bool immediately = false)
        {
            tabButton.Select(immediately);
            foreach (var buttonToDeselect in _tabButtons)
            {
                if(buttonToDeselect == tabButton)
                    continue;
                buttonToDeselect.Deselect(immediately);
            }

            _selectedItem.Value = tabButton.Id;
        }
    }
}
