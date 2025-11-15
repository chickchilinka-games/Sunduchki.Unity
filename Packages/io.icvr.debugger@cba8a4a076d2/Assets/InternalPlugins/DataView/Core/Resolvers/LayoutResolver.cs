using System.Collections.Generic;
using DebuggerPlugins.DataView.Core.Interfaces;
using Zenject;

namespace DebuggerPlugins.DataView.Core.Resolvers
{
    internal class LayoutResolver
    {
        private readonly DiContainer _diContainer;

        public LayoutResolver(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public IEnumerable<IDataViewLayout> Resolve()
        {
            return _diContainer.Resolve<IEnumerable<IDataViewLayout>>();
        }
    }
}