using UnityEngine;
using Zenject;

namespace Base.Pool.Factory
{
    public class ComponentFromPrefabFactory<T> : IFactory<T> where T : Component
    {
        private readonly DiContainer _container;
        private readonly GameObject _prefab;

        public ComponentFromPrefabFactory(GameObject prefab, DiContainer container)
        {
            _container = container;
            _prefab = prefab;
        }

        public T Create()
        {
            var obj = _container.InstantiatePrefabForComponent<T>(_prefab);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}
