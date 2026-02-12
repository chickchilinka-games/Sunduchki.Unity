using UnityEditor;
using UnityEngine;

namespace Modules.AssetSystem.Editor
{
    internal static class AssetContextMenu
    {
        private const string ResourcesPath = "Assets/Resources/";

        [MenuItem("Assets/Copy Resources Path", false, 20)]
        private static void CopyResourcesPath()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject == null) return;

            var assetPath = AssetDatabase.GetAssetPath(selectedObject);
            if (TryMakeResourcesPath(assetPath, out var resourcesPath))
            {
                EditorGUIUtility.systemCopyBuffer = resourcesPath;
                Debug.Log($"Copied Resources path: {resourcesPath}");
            }
            else
            {
                Debug.LogWarning($"Asset is not in Resources folder: {assetPath}");
            }
        }

        [MenuItem("Assets/Copy Resources Path", true)]
        private static bool ValidateCopyResourcesPath()
        {
            var selectedObject = Selection.activeObject;
            if (selectedObject == null) return false;

            var assetPath = AssetDatabase.GetAssetPath(selectedObject);
            return !string.IsNullOrEmpty(assetPath) && assetPath.StartsWith(ResourcesPath);
        }

        private static bool TryMakeResourcesPath(string assetPath, out string resourcesPath)
        {
            resourcesPath = null;
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(ResourcesPath))
                return false;

            var rel = assetPath.Substring(ResourcesPath.Length);
            resourcesPath = System.IO.Path.ChangeExtension(rel, null).Replace('\\', '/');
            return true;
        }
    }
}
