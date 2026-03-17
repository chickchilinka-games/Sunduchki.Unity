

using System.IO;
using ICVR.Window.Data;
using UnityEditor;
using UnityEngine;

namespace WindowSystemEditor
{
    [InitializeOnLoad]
    public class ConfigEditorHandler
    {
        static ConfigEditorHandler()
        {
            var config = Resources.Load(Consts.Name.ConfigAssetName);

            if (config != null)
            {
                return;
            }
            
            config = ScriptableObject.CreateInstance<WindowSystemConfig>();

            if (!Directory.Exists(Consts.Path.ResourceFolder))
            {
                Directory.CreateDirectory(Consts.Path.ResourceFolder);
            }
            
            AssetDatabase.CreateAsset(config, Path.Combine(Consts.Path.ResourceFolder, Consts.Name.ConfigAssetNameWithExtension));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}