using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Modules.AssetSystem.Storage
{
    internal class AssetCache
    {
        private class Entry
        {
            public Object Asset;
            public Type Type;
            public int RefCount;
        }

        private readonly Dictionary<string, Entry> _cache = new();

        public void CacheAsset<T>(string id, T asset) where T : Object
        {
            if (_cache.TryGetValue(id, out var entry))
            {
                if (entry.Type != typeof(T))
                    throw new InvalidOperationException(
                        $"Asset id '{id}' already cached as {entry.Type.Name}, requested as {typeof(T).Name}.");

                entry.RefCount++;
                return;
            }

            _cache[id] = new Entry
            {
                Asset = asset,
                Type = typeof(T),
                RefCount = 1
            };
        }

        public bool TryGetAsset<T>(string id, out T asset) where T : Object
        {
            asset = null;

            if (!_cache.TryGetValue(id, out var entry))
                return false;

            if (entry.Type != typeof(T))
                return false;

            if (entry.Asset is not T typed) 
                return false;
            
            entry.RefCount++;
            asset = typed;
            return true;
        }

        public int ReleaseAsset(string id, out Object assetOrNull)
        {
            assetOrNull = null;

            if (!_cache.TryGetValue(id, out var entry))
                return 0;

            var newCount = --entry.RefCount;
            if (newCount <= 0)
            {
                assetOrNull = entry.Asset;
                _cache.Remove(id);
                return 0;
            }

            return newCount;
        }
    }
}
