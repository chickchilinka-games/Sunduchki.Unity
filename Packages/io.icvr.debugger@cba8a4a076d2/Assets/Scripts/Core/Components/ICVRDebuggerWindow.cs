using System;
using System.Collections.Generic;
using System.Linq;
using Core.Enums;
using Core.Interfaces;
using Core.Model;
using Core.Services;
using TMPro;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Core.Components
{
    internal class ICVRDebuggerWindow : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private PageView pageView;
        [SerializeField] private WidgetView widgetView;

        [Header("Controls")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform tabsContainer;
        [SerializeField] private TMP_Dropdown widgetSelector;
        
        [SerializeField] private TMP_Text currentTab;
        
        [Header("Popups")]
        [SerializeField] private GameObject errorPopup;

        private LayoutManager _layoutManager;
        private InternalDebuggerService _debuggerService;
        private TabFactory _tabFactory;
        private TabConfig _tabConfig;

        private List<ILayout> _layouts;
        private Observable<Unit> _closeButtonClicked;
        private Observable<string> _selectedActiveWidget;
        private ReactiveProperty<string> _activePageId = new ReactiveProperty<string>();
        private ReactiveProperty<string> _activeWidgetId = new ReactiveProperty<string>();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly List<IResettableView> _resettableViews = new List<IResettableView>();


        [Inject]
        internal void Construct(LayoutManager layoutManager, InternalDebuggerService debuggerService, TabFactory tabFactory, TabConfig tabConfig)
        {
            _layoutManager = layoutManager;
            _debuggerService = debuggerService;
            _tabFactory = tabFactory;
            _tabConfig = tabConfig;
        }

        public void Initialize(List<ILayout> layouts)
        {
            _layouts = layouts;
            InitializeViews();
            InitializeControls();
            SubscribeToModels();
        }

        private void InitializeViews()
        {
            var pages = _layouts.Where(layout => layout.LayoutType == LayoutType.Page).ToList();
            pageView.Initialize(pages);
            foreach (var layout in pages)
            {
                layout.Pivot.SetParent(pageView.LayoutContainer, false);
                layout.Pivot.gameObject.SetActive(false);
            }

            var widgets = _layouts.Where(layout => layout.LayoutType == LayoutType.Widget).ToList();
            widgetView.Initialize(widgets);
            foreach (var layout in widgets)
            {
                layout.Pivot.SetParent(widgetView.LayoutContainer, false);
                layout.Pivot.gameObject.SetActive(false);
            }

            // Initialize Tabs
            InitializeTabs(pages);
        }

        private void InitializeTabs(List<ILayout> pages)
        {
            foreach (var layout in pages)
            {
                var tabEntry = _tabConfig.Tabs.FirstOrDefault(t => t.LayoutId == layout.Id);
                if (tabEntry == null)
                {
                    Debug.LogWarning($"No TabConfig entry found for LayoutId: {layout.Id}");
                    continue;
                }

                var tabInstance = _tabFactory.Create();
                // Set the parent to the tabs container
                tabInstance.transform.SetParent(tabsContainer, false);
                // Initialize the tab using data from TabConfig
                tabInstance.Initialize(tabEntry.TabName, tabEntry.TabIcon);
                // Subscribe to the tab's selection event
                tabInstance.OnTabSelected
                          .Subscribe(_ => OnTabSelected(layout.Id))
                          .AddTo(_disposables);
            }
        }

        private void InitializeControls()
        {
            _closeButtonClicked = closeButton.OnClickAsObservable();
            _selectedActiveWidget = SetupDropdown(widgetSelector, LayoutType.Widget);
        }

        private Observable<string> SetupDropdown(TMP_Dropdown dropdown, LayoutType layoutType)
        {
            var items = GetLayoutIds(layoutType);
            if (items.Count <= 1)
            {
                dropdown.gameObject.SetActive(false);
            }
            else
            {
                dropdown.gameObject.SetActive(true);
                dropdown.ClearOptions();
                dropdown.AddOptions(items);
            }

            return dropdown.onValueChanged.AsObservable().Select(ind => items[ind]);
        }

        private List<string> GetLayoutIds(LayoutType type)
        {
            return _layouts
                .Where(layout => layout.LayoutType == type)
                .Select(layout => layout.Id)
                .ToList();
        }

        private void SubscribeToModels()
        {
            _closeButtonClicked.Subscribe(_ => _debuggerService.HideDebugger()).AddTo(this);

            // Subscribe to layout manager's active page and widget
            _layoutManager.ActivePageId
                          .Subscribe(id => SetActiveTab(id))
                          .AddTo(this);

            _layoutManager.ActiveWidgetId
                          .Subscribe(id =>
                          {
                              if (!widgetView.DisplayWidget(id))
                                  ShowError($"Failed to display widget with ID: {id}");
                          })
                          .AddTo(widgetView);

            _debuggerService.VisibilityChanged.Subscribe(SetVisible).AddTo(this);
            _layoutManager.ActivePageId.Subscribe(id =>
            {
                var tabEntry = _tabConfig.Tabs.FirstOrDefault(t => t.LayoutId == id);
                currentTab.text = tabEntry?.TabName ?? "";
                if (!pageView.DisplayPage(id))
                {
                    ShowError($"Failed to display page with ID: {id}");
                }
            }).AddTo(pageView);
            _layoutManager.ActiveWidgetId.Subscribe(id =>
            {
                if (!widgetView.DisplayWidget(id))
                    ShowError($"Failed to display widget with ID: {id}");
            }).AddTo(widgetView);
        }

        private void OnTabSelected(string layoutId)
        {
            _layoutManager.SetActivePage(layoutId);
        }

        private void SetActiveTab(string activeId)
        {
            foreach (Transform child in tabsContainer)
            {
                if (child.TryGetComponent<PageTab>(out var tab))
                {
                    // Compare the LayoutId with the activeId
                    var tabEntry = _tabConfig.Tabs.FirstOrDefault(t => t.TabName == tab.LabelText); // Assuming LabelText holds LayoutId
                    bool isActive = tabEntry != null && tabEntry.LayoutId == activeId;
                    tab.SetActive(isActive);
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (!visible)
            {
                ResetChildViews();
            }

            gameObject.SetActive(visible);
        }

        private void ResetChildViews()
        {
            _resettableViews.Clear();
            GetComponentsInChildren(true, _resettableViews);

            foreach (var resettable in _resettableViews)
            {
                if (resettable == null)
                {
                    continue;
                }

                try
                {
                    resettable.ResetView();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            _resettableViews.Clear();
        }

        private void ShowError(string message)
        {
            errorPopup.SetActive(true);
            // Optionally display the error message
            var errorText = errorPopup.GetComponentInChildren<TMP_Text>();
            if (errorText != null)
            {
                errorText.text = message;
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}