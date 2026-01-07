using System;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Services;
using UnityEngine;
using Object = UnityEngine.Object;

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
            if (asset == null && typeof(T) == typeof(Sprite))
            {
                var sprite = TryLoadSpriteFromFolder(assetId);
                if (sprite != null)
                {
                    asset = sprite as T;
                }
            }
            return UniTask.FromResult(asset);
        }

        public UniTask Unload<T>(T asset) where T : Object
        {
            _assetUnloadService.Record(asset);
            return UniTask.CompletedTask;
        }

        private static Sprite TryLoadSpriteFromFolder(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return null;
            }

            var normalized = assetId.Replace('\\', '/');
            var lastSlash = normalized.LastIndexOf('/');
            if (lastSlash <= 0 || lastSlash >= normalized.Length - 1)
            {
                return null;
            }

            var folder = normalized.Substring(0, lastSlash);
            var spriteName = normalized.Substring(lastSlash + 1);
            var sprites = Resources.LoadAll<Sprite>(folder);
            foreach (var sprite in sprites)
            {
                if (sprite != null && string.Equals(sprite.name, spriteName, StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
