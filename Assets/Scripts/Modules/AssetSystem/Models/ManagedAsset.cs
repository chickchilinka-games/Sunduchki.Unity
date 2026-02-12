using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modules.AssetSystem.Models
{
    public class ManagedAsset<T> : IUniTaskAsyncDisposable, IDisposable where T : Object
    {
        private readonly AssetService _service;
        private readonly string _id;
        private bool _disposed;

        public T Asset { get; private set; }

        public ManagedAsset(T asset, AssetService service, string id)
        {
            Asset = asset;
            _service = service;
            _id = id;
        }

        public async UniTask DisposeAsync()
        {
            if (_disposed)
                return;
            _disposed = true;

            Asset = null;
            
            try
            {
                await _service.ReleaseAsset<T>(_id);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        public void Dispose()
        {
            DisposeAsync().Forget();   
        }

#if UNITY_EDITOR
        ~ManagedAsset()
        {
            if (!_disposed)
                Debug.LogWarning($"ManagedAsset<{typeof(T).Name}> for '{_id}' was not disposed.");
        }
#endif
    }
}
