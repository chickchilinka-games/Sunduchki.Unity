using Core.Rules;
using DebuggerPlugins.DataView.Core.Resolvers;
using DebuggerPlugins.DataView.Core.Services;
using InternalPlugins.DataView.Core.Storages;
using Zenject;

namespace InternalPlugins.DataView.Core.Rules
{
    internal class ResolveRule : IRule, IInitializable
    {
        private readonly DataViewService _dataViewService;
        private readonly LayoutResolver _layoutResolver;
        private readonly DataResolver _dataResolver;
        private readonly LayoutsStorage _layoutsStorage;

        public ResolveRule(DataViewService dataViewService, LayoutResolver layoutResolver,
            DataResolver dataResolver, LayoutsStorage layoutsStorage)
        {
            _dataViewService = dataViewService;
            _layoutResolver = layoutResolver;
            _dataResolver = dataResolver;
            _layoutsStorage = layoutsStorage;
        }

        public void Initialize()
        {
            var layouts = _layoutResolver.Resolve();
            _layoutsStorage.Add(layouts);

            var stringDatas = _dataResolver.ResolveStringData();
            foreach (var stringData in stringDatas)
            {
                _dataViewService.AddStringData(stringData);
            }
        }
    }
}