

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zenject;
using IPrefabProvider = ICVR.Window.Interfaces.IPrefabProvider;

namespace ICVR.Window.PrefabProviders
{
    public class AddressablePrefabProvider : IInitializable, IPrefabProvider
    {
        public int Priority => 50;
        private bool _isAvailable;

        public void Initialize()
        {
            var handle = Addressables.InitializeAsync();
            handle.Completed += OnInitialized;
        }

        private void OnInitialized(AsyncOperationHandle<IResourceLocator> handle)
        {
            _isAvailable = true;
            ResourceManager.ExceptionHandler = null;
        }

        public UniTask<GameObject> GetContentPrefab(string id)
        {
            return _isAvailable ? Addressables.LoadAssetAsync<GameObject>(id).ToUniTask() : UniTask.FromResult<GameObject>(default);
        }

        public UniTask<GameObject> GetTemplatePrefab(string id)
        {
            return _isAvailable ? Addressables.LoadAssetAsync<GameObject>(id).ToUniTask() : UniTask.FromResult<GameObject>(default);
        }
        
    }
}