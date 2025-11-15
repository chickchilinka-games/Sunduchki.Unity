using System;
using System.Collections.Generic;
using Modules.AssetSystem.Editor.AssetPicker.Data;

namespace Modules.AssetSystem.Editor.AssetPicker.Providers
{
    internal interface IAssetRefProvider
    {
        string DisplayName { get; }

        IEnumerable<AssetRefEntry> Query(Type type);

        bool TryMakeAssetRef(UnityEngine.Object obj, out string assetRef);
        bool TryResolve(string assetRef, Type type, out UnityEngine.Object obj, out string assetPath);
    }
}