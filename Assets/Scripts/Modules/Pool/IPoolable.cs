using UnityEngine;

namespace Pool
{
    public interface IPoolable
    {
        object ObjectId { get; }
        GameObject GameObject { get; }
        
        void InitializePoolable(object objectId);
        void OnSpawn(Transform parent);
        void ReInitialize();
        void OnDespawn(Transform parent);
    }
}
