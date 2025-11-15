using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Providers;

namespace Modules.AssetSystem.Editor.Tests
{
    public class MockAssetProvider : IAssetProvider
    {
        private readonly Dictionary<string, UnityEngine.Object> _assets = new();
        private readonly List<string> _unloadedAssets = new();
        
        public bool ThrowOnGet { get; set; }
        public bool ThrowOnUnload { get; set; }
        public int GetCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }
        public List<string> UnloadedAssets => _unloadedAssets;

        public void SetAsset<T>(string id, T asset) where T : UnityEngine.Object
        {
            _assets[id] = asset;
        }

        public void RemoveAsset(string id)
        {
            _assets.Remove(id);
        }

        public void Clear()
        {
            _assets.Clear();
            _unloadedAssets.Clear();
            GetCallCount = 0;
            UnloadCallCount = 0;
        }

        public async UniTask<T> Get<T>(string assetId) where T : UnityEngine.Object
        {
            GetCallCount++;
            
            if (ThrowOnGet)
                throw new Exception("Mock provider get error");

            await UniTask.Yield();

            if (_assets.TryGetValue(assetId, out var asset) && asset is T typedAsset)
                return typedAsset;

            return null;
        }

        public async UniTask Unload<T>(T asset) where T : UnityEngine.Object
        {
            UnloadCallCount++;
            
            if (ThrowOnUnload)
                throw new Exception("Mock provider unload error");

            await UniTask.Yield();

            foreach (var kvp in _assets)
            {
                if (kvp.Value == asset)
                {
                    _unloadedAssets.Add(kvp.Key);
                    break;
                }
            }
        }
    }
}