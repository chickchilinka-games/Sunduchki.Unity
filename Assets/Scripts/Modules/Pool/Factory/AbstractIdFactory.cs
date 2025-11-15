using System;
using System.Collections.Generic;
using Zenject;

namespace Base.Pool.Factory
{
    public class AbstractIdFactory<TType, TEnum>  where TEnum : Enum
    {
        private readonly DiContainer _diContainer;

        public AbstractIdFactory(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public virtual TType Spawn(TEnum enumId)
        {
            return _diContainer.ResolveId<TType>(enumId);
        }
        
        public bool HasBinding(TEnum enumId)
        {
            return _diContainer.HasBindingId<TType>(enumId);
        }
    }
    
    public class AbstractIdFactoryWithCache<TType, TEnum>  : AbstractIdFactory<TType, TEnum>
        where TEnum : Enum
    {
        private readonly Dictionary<TEnum, TType> _cache = new Dictionary<TEnum, TType>();
        
        public AbstractIdFactoryWithCache(DiContainer diContainer) : base(diContainer)
        {
        }

        public override TType Spawn(TEnum enumId)
        {
            if (_cache.ContainsKey(enumId))
                return _cache[enumId];
            
            var handler = base.Spawn(enumId);
            
            _cache.Add(enumId, handler);

            return handler;
        }
    }
    
    public class AbstractIdClassFactory<TType, TClass>
    {
        private readonly DiContainer _diContainer;

        public AbstractIdClassFactory(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public TType Spawn()
        {
            return _diContainer.ResolveId<TType>(typeof(TClass).Name);
        }
    }
    
    public class AbstractIdFactory<TType>
    {
        private readonly DiContainer _diContainer;

        public AbstractIdFactory(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public TType Spawn(string id)
        {
            return _diContainer.ResolveId<TType>(id);
        }
    }
}