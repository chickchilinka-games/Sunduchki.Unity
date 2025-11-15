using System.Collections.Generic;
using Zenject;

namespace Base.Pool.Factory
{
    public class AbstractFactory<TType>
    {
        private readonly DiContainer _diContainer;

        protected AbstractFactory(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public TType Spawn()
        {
            return _diContainer.Resolve<TType>();
        }
        
        public List<TType> SpawnAll()
        {
            return _diContainer.ResolveAll<TType>();
        }
    }
}