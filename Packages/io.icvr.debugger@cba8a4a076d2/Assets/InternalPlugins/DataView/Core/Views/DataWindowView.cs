using Assets.InternalPlugins.DataView.Paggination;
using Core.Enums;
using Core.Interfaces;
using DebuggerPlugins.DataView.Core.Services;
using UnityEngine;
using Zenject;

namespace DebuggerPlugins.DataView.Core.Views
{
    internal class DataWindowView : MonoBehaviour, ILayout
    {
        [Header("ILayout fields")]
        [SerializeField] private string _id;
        [SerializeField] private LayoutType _layoutType;
        [SerializeField] private RectTransform _pivot;

        [SerializeField] private RectTransform _container;

        public string Id => _id;
        public LayoutType LayoutType => _layoutType;
        public RectTransform Pivot => _pivot;

        private PaginationController _paginationController; 
        private DataViewService _dataViewService;

        [Inject]
        private void Construct(DataViewService dataViewService)
        {
            _dataViewService = dataViewService;
        }

        private void Start()
        {
            _paginationController = _container.GetComponent<PaginationController>();
            
            ResolveLayers();
        }
        
        private void ResolveLayers()
        {
            var dataLayouts = _dataViewService.GetLayouts();
            foreach (var dataLayout in dataLayouts)
            {
                var layout = dataLayout.Pivot;
                _paginationController.AddContent(layout);
            }
        }
    }
}