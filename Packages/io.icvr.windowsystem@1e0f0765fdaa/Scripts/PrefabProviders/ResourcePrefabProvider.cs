// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

using Cysharp.Threading.Tasks;
using ICVR.Window.Abstract;
using ICVR.Window.Basics;
using ICVR.Window.Data;
using ICVR.Window.Interfaces;
using UnityEngine;

namespace ICVR.Window.PrefabProviders
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