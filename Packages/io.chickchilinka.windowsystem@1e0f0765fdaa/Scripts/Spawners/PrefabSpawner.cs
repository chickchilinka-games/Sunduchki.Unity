

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Chickchilinka.Window.Data;
using Chickchilinka.Window.Storages;
using UnityEngine;
using Zenject;
using IPrefabProvider = Chickchilinka.Window.Interfaces.IPrefabProvider;

namespace Chickchilinka.Window.Spawners
{
    internal class PrefabSpawner
    {
        private List<IPrefabProvider> _providers;
        protected ContainersStorage ContainerStorage;
        
        private Dictionary<string, string> _contentMap;
        private Dictionary<string, string> _templateMap;

        [Inject]
        protected void Construct(List<IPrefabProvider> providers, ContainersStorage containerStorage, WindowSystemConfig config)
        {
            _contentMap = config.ContentMap.ToDictionary(data => data.TypeFullName, data => data.Id);
            _templateMap = config.TemplateMap.ToDictionary(data => data.TypeFullName, data => data.Id);
            ContainerStorage = containerStorage;
            _providers = providers.OrderByDescending(item => item.Priority).ToList();
        }

        public string GetContentIdByType(Type type) => GetIdByName(type, _contentMap);
        public string GetTemplateIdByType(Type type) => GetIdByName(type, _templateMap);

        private string GetIdByName(Type type, Dictionary<string, string> map)
        {
            var fullName = type.FullName;
            if (string.IsNullOrEmpty(fullName))
            {
                Debug.LogError($"{type} full name is null or empty.");
                return type.Name;
            }
            return map.TryGetValue(fullName, out var value) ? value : type.Name;
        }

        protected bool TryInstantiateWindowComponent<TWindowComponent>(GameObject prefab,
            object[] args,
            out TWindowComponent outComponent)
            where TWindowComponent : MonoBehaviour
        {
            outComponent = default;
            if (prefab == null)
            {
                Debug.LogError("Prefab is null");
                return false;
            }
            if (!prefab.TryGetComponent(out TWindowComponent prefabComponent))
            {
                Debug.LogError($"Prefab {prefab.name}.prefab doesn't have {typeof(TWindowComponent).Name} component or t's descendent.");
                return false;
            }

            var componentType = prefabComponent.GetType();
            var container = ContainerStorage.GetContainerFor(componentType);
            try
            {
                outComponent = args == null || args.Length == 0
                    ? container.InstantiatePrefabForComponent<TWindowComponent>(prefab)
                    : container.InstantiatePrefabForComponent<TWindowComponent>(prefab, args);
                return true;
            }
            catch (ZenjectException ex)
            {
                Debug.LogError($"Failed to instantiate {prefab.name}.prefab of type {componentType.Name}.\n" +
                               "If your component use nested dependencies in child context," +
                               $"use <color=green>Container.BindWindowContent<{componentType.Name}>()</color> or <color=green>BindWindowTemplate<{componentType.Name}>()</color>." +
                               $"Exception:\n{ex}");
                return false;
            }
        }

        protected async UniTask<GameObject> GetContentPrefab(string id)
        {
            foreach (var provider in _providers)
            {
                try
                {
                    var prefab = await provider.GetContentPrefab(id);
                    if (prefab)
                        return prefab;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Prefab provider `{provider.GetType().Name}` content failed:\n{ex}");
                }
            }
            return null;
        }

        protected async UniTask<GameObject> GetTemplatePrefab(string id)
        {
            foreach (var provider in _providers)
            {
                try
                {
                    var prefab = await provider.GetTemplatePrefab(id);
                    if (prefab)
                        return prefab;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Prefab provider `{provider.GetType().Name}` template failed:\n{ex}");
                }
            }
            return null;
        }
    }
}