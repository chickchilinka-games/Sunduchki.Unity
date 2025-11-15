using System;
using UnityEngine;

namespace Modules.AssetSystem.Attributes
{
    [Serializable]
    public struct AssetRef<T>
    {
        [SerializeField] public string Path;
        [SerializeField] public string GUID;
    }
}