using System.Collections.Generic;
using Core.Interfaces;
using Zenject;

namespace Core.Factories
{
    internal class PluginResolver
    {
        private readonly DiContainer _diContainer;

        public PluginResolver(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public List<IPlugin> Resolve()
        {
            return _diContainer.Resolve<List<IPlugin>>();
        }
    }
}