using System.Collections.Generic;
using Core.Factories;
using Core.Interfaces;

namespace Core.Services
{
    internal class PluginService
    {
        private readonly PluginResolver _pluginResolver;

        public PluginService(PluginResolver pluginResolver)
        {
            _pluginResolver = pluginResolver;
        }

        public List<IPlugin> GetPlugins()
        {
            return _pluginResolver.Resolve();
        }
    }
}