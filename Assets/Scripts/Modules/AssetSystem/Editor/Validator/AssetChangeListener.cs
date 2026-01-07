using Modules.AssetSystem.Editor.AssetPicker.View;
using UnityEditor;

namespace Modules.AssetSystem.Editor.Validator
{
    public class AssetChangeListener : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (EditorWindow.HasOpenInstances<AssetRefValidatorWindow>())
            {
                //var wnd = EditorWindow.GetWindow<AssetRefValidatorWindow>();
                //if (wnd)
                    //wnd.RebuildNow();
            }
        }
    }

}