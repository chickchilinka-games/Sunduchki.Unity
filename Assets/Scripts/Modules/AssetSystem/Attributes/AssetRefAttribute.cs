using System;
using UnityEngine;

namespace Modules.AssetSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class AssetRefAttribute : PropertyAttribute
    {
        public Type TargetType { get; }

        public AssetRefAttribute(Type targetType)
        {
            if (targetType == null || !typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                throw new ArgumentException("AssetRefAttribute requires a UnityEngine.Object type.");
            TargetType = targetType;
        }
    }
}
