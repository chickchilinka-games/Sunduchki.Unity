// ICVR CONFIDENTIAL
// __________________
// 
// [2016] - [2023] ICVR LLC
// All Rights Reserved.
// 
// NOTICE:  All information contained herein is, and remains
// the property of ICVR LLC and its suppliers,
// if any.  The intellectual and technical concepts contained
// herein are proprietary to ICVR LLC
// and its suppliers and may be covered by U.S. and Foreign Patents,
// patents in process, and are protected by trade secret or copyright law.
// Dissemination of this information or reproduction of this material
// is strictly forbidden unless prior written permission is obtained
// from ICVR LLC.

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