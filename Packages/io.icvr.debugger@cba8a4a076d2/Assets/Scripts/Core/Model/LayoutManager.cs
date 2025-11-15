using System.Linq;
using Core.Enums;
using Core.Services;
using R3;

namespace Core.Model
{
    internal class LayoutManager
    {
        public ReadOnlyReactiveProperty<string> ActivePageId => _activePageId;
        public ReadOnlyReactiveProperty<string> ActiveWidgetId => _activeWidgetId;

        private readonly ReactiveProperty<string> _activePageId;
        private readonly ReactiveProperty<string> _activeWidgetId;

        private readonly LayoutService _layoutService;

        public LayoutManager(LayoutService layoutService)
        {
            _layoutService = layoutService;

            _activePageId = new ReactiveProperty<string>();
            _activeWidgetId = new ReactiveProperty<string>();
        }

        public void Initialize()
        {
            var layouts = _layoutService.GetLayouts();

            _activePageId.Value = layouts.FirstOrDefault(layout => layout.LayoutType.Equals(LayoutType.Page))?.Id ?? string.Empty;
            _activeWidgetId.Value = layouts.FirstOrDefault(layout => layout.LayoutType.Equals(LayoutType.Widget))?.Id ?? string.Empty;
        }

        public void SetActivePage(string id)
        {
            _activePageId.Value = id;
        }

        public void SetActiveWidget(string id)
        {
            _activeWidgetId.Value = id;
        }
    }
}