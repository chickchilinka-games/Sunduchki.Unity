

using Cysharp.Threading.Tasks;
using Chickchilinka.Window.Abstract;
using Chickchilinka.Window.Basics;
using Chickchilinka.Window.Data;
using Chickchilinka.Window.Interfaces;
using UnityEngine;

namespace Chickchilinka.Window.PrefabProviders
{
    internal class ResourcePrefabProvider : IPrefabProvider
    {
        public int Priority => 20;

        private readonly AbstractContent[] _resourceContentObjects;
        private readonly AbstractTemplate[] _resourceTemplateObjects;

        public ResourcePrefabProvider(WindowSystemConfig config)
        {
            _resourceContentObjects = Resources.LoadAll<AbstractContent>(config.ResourcePrefabsRootPath);
            _resourceTemplateObjects = Resources.LoadAll<AbstractTemplate>(config.ResourcePrefabsRootPath);
        }

        public UniTask<GameObject> GetContentPrefab(string id)
        {
            var objectsCount = _resourceContentObjects.Length;
            for (var i = 0; i < objectsCount; i++)
            {
                if (_resourceContentObjects[i].Id.Equals(id))
                    return UniTask.FromResult(_resourceContentObjects[i].gameObject);
            }
            Debug.LogError($"Content was not found by id ({id})! Check that is added to the Resource folder with the AbstractContent-inherited component.");
            return UniTask.FromResult<GameObject>(default);
        }

        public UniTask<GameObject> GetTemplatePrefab(string id)
        {
            var objectsCount = _resourceTemplateObjects.Length;
            for (var i = 0; i < objectsCount; i++)
            {
                if (_resourceTemplateObjects[i].Id.Equals(id))
                    return UniTask.FromResult(_resourceTemplateObjects[i].gameObject);
            }
            
            Debug.LogError($"Template was not found by id ({id})! Check that is added to the Resource folder with the AbstractTemplate-inherited component.");
            return UniTask.FromResult<GameObject>(default);
        }
    }
}