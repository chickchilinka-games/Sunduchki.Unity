using Chickchilinka.Tools;
using UnityEditor;
using UnityEngine;

namespace CI.Editor
{
    public class CICustomConfig: ICICustomConfig
    {
        public void ApplyCustomSetup()
        {
            
        }

        public string[] GetSceneListForTarget(BuildTarget buildTarget)
        {
            return null;
        }

        public void PostProcessBuild()
        {
            Debug.Log("PostProcessBuild");
        }

        public void ApplyEnvironment(AppEnvironment environment, IConfigProvider configProvider)
        {
            Debug.Log($"ApplyEnvironment: {environment}");
            
            Debug.Log("Processing custom environment: " + environment);
            
            PlayerSettings.applicationIdentifier = environment == AppEnvironment.Prod ? "" : "com.chickchilinka.sunduchki";

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
