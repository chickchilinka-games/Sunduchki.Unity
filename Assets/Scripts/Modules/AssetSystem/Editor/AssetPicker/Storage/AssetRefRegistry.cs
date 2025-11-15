using System;
using System.Collections.Generic;
using System.Linq;
using Modules.AssetSystem.Editor.AssetPicker.Data;
using Modules.AssetSystem.Editor.AssetPicker.Providers;

namespace Modules.AssetSystem.Editor.AssetPicker.Storage
{
    internal static class AssetRefRegistry
    {
        private static readonly List<IAssetRefProvider> Providers = new()
        {
            new ResourcesAssetRefProvider()
        };

        public static IEnumerable<AssetRefEntry> Query(Type type)
        {
            return Providers.SelectMany(provider => provider.Query(type));
        }

        public static bool TryResolve(string assetRef, Type type, out UnityEngine.Object obj, out string path)
        {
            foreach (var p in Providers)
                if (p.TryResolve(assetRef, type, out obj, out path))
                    return true;
            obj = null;
            path = null;
            return false;
        }

        public static bool TryMakeAssetRef(UnityEngine.Object obj, out string assetRef)
        {
            foreach (var p in Providers)
                if (p.TryMakeAssetRef(obj, out assetRef))
                    return true;
            assetRef = null;
            return false;
        }
    }
}