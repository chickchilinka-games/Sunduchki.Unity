using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Core.Editor
{
    internal class AssetsFinder
    {
        public static T[] GetAllInstances<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .ToArray();
        }
    }
}
#endif