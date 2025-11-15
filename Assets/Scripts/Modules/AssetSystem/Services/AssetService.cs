using System;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Exceptions;
using Modules.AssetSystem.Models;
using Modules.AssetSystem.Providers;
using Modules.AssetSystem.Storage;
using Object = UnityEngine.Object;

namespace Modules.AssetSystem.Services
{
    public class AssetService
    {
        private readonly IAssetProvider _provider;
        private readonly AssetCache _cache;
        
        internal AssetService(IAssetProvider provider, AssetCache cache)
        {
            _provider = provider;
            _cache = cache;
        }

        public async UniTask<ManagedAsset<T>> Get<T>(string assetId) where T : Object
        {
            if (_cache.TryGetAsset(assetId, out T cached))
                return new ManagedAsset<T>(cached, this, assetId);

            var asset = await _provider.Get<T>(assetId);
            if (asset == null)
                throw new AssetNotFoundException(assetId, typeof(T));

            _cache.CacheAsset(assetId, asset);
            return new ManagedAsset<T>(asset, this, assetId);
        }

        internal async UniTask ReleaseAsset<T>(string assetId) where T : Object
        {
            var count = _cache.ReleaseAsset(assetId, out Object lastAsset);
            if (count == 0 && lastAsset != null)
            {
                try
                {
                    await _provider.Unload((T)lastAsset);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }
    }
}