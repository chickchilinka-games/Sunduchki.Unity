using System.Collections.Generic;
using Base.Pool.Factory;
using Pool;
using UnityEngine;
using Zenject;
using IPoolable = Pool.IPoolable;

namespace Base.Pool.Services
{
    public class GeneralPool
    {
        private readonly Transform _inactiveObjectsContainer;
        private readonly DiContainer _diContainer;
        
        private readonly Dictionary<object, IMemoryPool> _pools = new Dictionary<object, IMemoryPool>();
        
        public GeneralPool(DiContainer diContainer)
        {
            _diContainer = diContainer;

            _inactiveObjectsContainer = new GameObject(GetType().Name).transform;
            GameObject.DontDestroyOnLoad(_inactiveObjectsContainer.gameObject);
        }
        
        public TPoolable Spawn<TPoolable>(Transform parent) where TPoolable : IPoolable
        {
            return Spawn<TPoolable>(parent, typeof(TPoolable));
        }
        
        public TPoolable Spawn<TPoolable>(Transform parent, object objectId) where TPoolable : IPoolable
        {
            var pool = GetPool<TPoolable>(objectId);
            
            if (pool == null)
            {
                // Debug.LogError("Pool is null when spawning " + objectId);
                return default(TPoolable);
            }
            
            var poolable = pool.Spawn(parent);
            poolable.InitializePoolable(objectId);

            return poolable;
        }

        public void Despawn<TPoolable>(TPoolable poolable) where TPoolable : IPoolable
        {
            if (poolable == null)
                return;

            var pool = GetPool<TPoolable>(poolable.ObjectId);

            if (pool == null)
            {
                Debug.LogError("Pool is null when despawning " + poolable.ObjectId);
                return;
            }

            if (poolable is Object && (poolable as Object) == null)
            {
                Debug.LogWarning(poolable.ObjectId + " was destroyed");
                return;
            }

            pool.Despawn(poolable, _inactiveObjectsContainer);
        }

        public void DespawnAll<TPoolable>(IEnumerable<TPoolable> enumerable) where TPoolable : IPoolable
        {
            foreach (var poolable in enumerable)
                Despawn(poolable);

            if (enumerable is ICollection<TPoolable> collection)
                collection.Clear();
        }
        
        public void DestroyAll<TPoolable>() where TPoolable : IPoolable
        {
            var pool = GetPool<TPoolable>(typeof(TPoolable));
            pool?.DestroyAll();
        }
        
        private Pool<TPoolable> GetPool<TPoolable>(object id) where TPoolable : IPoolable
        {
            if (_pools.ContainsKey(id))
            {
                return _pools[id] as Pool<TPoolable>;
            }

            var pool = _diContainer.TryResolveId<Pool<TPoolable>>(id);

            if (pool == null)
                return null;
            
            _pools.Add(id, pool);

            return pool;
        }
    }
}
