using System;
using System.Collections.Generic;
using Core.Components;
using Core.Interfaces;
using Core.Model;
using Core.Services;
using Zenject;

namespace Core.Rules
{
    internal class InitializeRule : IRule, IInitializable, IDisposable
    {
        private readonly PluginService _pluginService;
        private readonly LayoutService _layoutService;
        private readonly ICVRDebuggerModel _icvrDebuggerModel;
        private readonly LayoutManager _layoutManager;
        private readonly IFactory<List<ILayout>, ICVRDebuggerWindow> _uiLayoutFactory;

        public InitializeRule(PluginService pluginService,
            LayoutService layoutService,
            ICVRDebuggerModel icvrDebuggerModel,
            LayoutManager layoutManager,
            IFactory<List<ILayout>, ICVRDebuggerWindow> uiLayoutFactory)
        {
            _pluginService = pluginService;
            _layoutService = layoutService;
            _icvrDebuggerModel = icvrDebuggerModel;
            _layoutManager = layoutManager;
            _uiLayoutFactory = uiLayoutFactory;
        }

        public void Initialize()
        {
            foreach (var plugin in _pluginService.GetPlugins())
            {
                plugin.Initialize();
            }

            _icvrDebuggerModel.Initialize();
            _layoutManager.Initialize();
            _uiLayoutFactory.Create(_layoutService.GetLayouts());
        }

        public void Dispose()
        {
            foreach (var plugin in _pluginService.GetPlugins())
            {
                plugin.Dispose();
            }
        }
    }
}