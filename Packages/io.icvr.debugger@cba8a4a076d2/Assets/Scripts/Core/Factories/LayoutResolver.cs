using System.Collections.Generic;
using Core.Interfaces;
using Zenject;

namespace Core.Factories
{
    internal class LayoutResolver
    {
        private readonly DiContainer _diContainer;

        public LayoutResolver(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public List<ILayout> Resolve()
        {
            return _diContainer.Resolve<List<ILayout>>();
        }
    }
}