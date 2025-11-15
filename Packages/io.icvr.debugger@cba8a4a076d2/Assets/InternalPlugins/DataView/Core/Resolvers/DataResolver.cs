using System.Collections.Generic;
using DebuggerPlugins.DataView.Core.Interfaces;
using Zenject;

namespace DebuggerPlugins.DataView.Core.Resolvers
{
    internal class DataResolver
    {
        private readonly DiContainer _diContainer;

        public DataResolver(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public IEnumerable<IStringData> ResolveStringData()
        {
            return _diContainer.Resolve<IEnumerable<IStringData>>();
        }
    }
}