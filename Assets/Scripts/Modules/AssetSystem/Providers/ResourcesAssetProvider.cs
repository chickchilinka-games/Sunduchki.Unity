using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Services;
using UnityEngine;

namespace Modules.AssetSystem.Providers
{
    internal sealed class ResourcesAssetProvider : IAssetProvider
    {
        private readonly AssetUnloadService _assetUnloadService;

        public ResourcesAssetProvider(AssetUnloadService assetUnloadService)
        {
            _assetUnloadService = assetUnloadService;
        }

        public UniTask<T> Get<T>(string assetId) where T : Object
        { 
            var asset = Resources.Load<T>(assetId);
            return UniTask.FromResult(asset);
        }

        public UniTask Unload<T>(T asset) where T : Object
        {
            _assetUnloadService.Record(asset);
            return UniTask.CompletedTask;
        }
    }
}