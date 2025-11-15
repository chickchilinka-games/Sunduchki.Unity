using System;
using System.Collections.Generic;
using System.IO;
using Modules.AssetSystem.Editor.AssetPicker.Data;
using UnityEditor;

namespace Modules.AssetSystem.Editor.AssetPicker.Providers
{
    internal sealed class ResourcesAssetRefProvider : IAssetRefProvider
    {
        public string DisplayName => "Resources";

        private const string ResourcesPath = "Assets/Resources/";

        public IEnumerable<AssetRefEntry> Query(Type type)
        {
            var guids = AssetDatabase.FindAssets($"t:{type.Name}", new[] { ResourcesPath });
            foreach (var g in guids)
            {
                var ap = AssetDatabase.GUIDToAssetPath(g);
                if (!TryMakeResourcesPath(ap, out var resourcesPath)) continue;

                var obj = AssetDatabase.LoadAssetAtPath(ap, type);
                yield return new AssetRefEntry
                {
                    AssetRef = resourcesPath,
                    Provider = DisplayName,
                    Folder = Path.GetDirectoryName(resourcesPath)?.Replace('\\', '/') ?? "",
                    Name = Path.GetFileName(resourcesPath),
                    AssetObject = obj
                };
            }
        }

        public bool TryMakeAssetRef(UnityEngine.Object obj, out string assetRef)
        {
            assetRef = null;
            if (obj == null) return false;

            var path = AssetDatabase.GetAssetPath(obj);
            if (!TryMakeResourcesPath(path, out var resourcesPath)) return false;

            assetRef = resourcesPath;
            return true;
        }

        public bool TryResolve(string assetRef, Type type, out UnityEngine.Object obj, out string assetPath)
        {
            obj = null;
            assetPath = null;

            if (!TryResolveResources(assetRef, type, out assetPath)) 
                return false;
            obj = AssetDatabase.LoadAssetAtPath(assetPath, type);
            return obj != null;
        }

        private static bool TryMakeResourcesPath(string assetPath, out string resourcesPath)
        {
            resourcesPath = null;
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(ResourcesPath))
                return false;

            var rel = assetPath.Substring(ResourcesPath.Length);
            resourcesPath = Path.ChangeExtension(rel, null).Replace('\\', '/');
            return true;
        }

        private static bool TryResolveResources(string resourcesPath, Type t, out string assetPath)
        {
            assetPath = null;
            var targetBase = ResourcesPath + resourcesPath;

            var guids = AssetDatabase.FindAssets($"t:{t.Name}", new[] { ResourcesPath });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var noExt = Path.ChangeExtension(path, null).Replace('\\', '/');
                if (noExt != targetBase) 
                    continue;
                
                assetPath = path;
                return true;
            }

            return false;
        }
    }
}