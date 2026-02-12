using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Observables;
using R3;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Modules.AssetSystem.Services
{
    public class AssetUnloadService : IInitializable, IDisposable
    {
        private readonly IEnumerable<AssetUnloadObservable> _triggerObservables;
        private readonly List<Object> _assetsToUnload = new();

        private IDisposable _unloadDisposable;

        private AssetUnloadService(IEnumerable<AssetUnloadObservable> observables)
        {
            _triggerObservables = observables;
        }
        
        public void Initialize()
        {
            _unloadDisposable = _triggerObservables
                .Merge()
                .Subscribe(async _ =>
                {
                    await UnloadAll();
                });
        }

        private async UniTask UnloadAll()
        {
            Debug.Log($"[{nameof(AssetUnloadService)}] Unloading assets");
            foreach (var asset in _assetsToUnload)
            {
                await Unload(asset);
            }
        }
        
        private UniTask Unload<T>(T asset) where T : Object
        {
            try
            {
                if (asset is GameObject)
                {
                    return Resources.UnloadUnusedAssets().ToUniTask();
                }

                Resources.UnloadAsset(asset);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return UniTask.CompletedTask;
        }
        
        public void Record<T>(T asset) where T : Object
        {
            _assetsToUnload.Add(asset);
        }

        public void Dispose()
        {
            _unloadDisposable?.Dispose();
        }
    }
}
