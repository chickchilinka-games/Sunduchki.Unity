/*
    ICVR CONFIDENTIAL
    __________________

    [2016] - [2022] ICVR LLC
    All Rights Reserved.

    NOTICE:  All information contained herein is, and remains
    the property of ICVR LLC and its suppliers,
    if any.  The intellectual and technical concepts contained
    herein are proprietary to ICVR LLC
    and its suppliers and may be covered by U.S. and Foreign Patents,
    patents in process, and are protected by trade secret or copyright law.
    Dissemination of this information or reproduction of this material
    is strictly forbidden unless prior written permission is obtained
    from ICVR LLC.
*/


using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ICVR.Tools
{
    [InitializeOnLoad]
    // ReSharper disable once InconsistentNaming
    public static class CIBuildMenu
    {
        private const string DevBuildKey = "CIBuildMenu.DevBuild";
        private const string DevMenuName = "Custom/CI/Development Build";

        private const string AutoRunKey = "CIBuildMenu.AutoRun";
        private const string AutoRunMenuName = "Custom/CI/Auto Run";

        static CIBuildMenu()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            var isDevelopment = EditorPrefs.GetBool(DevBuildKey);
            EditorUserBuildSettings.development = isDevelopment;
            Menu.SetChecked(DevMenuName, isDevelopment);

            var autoRunOnBuild = EditorPrefs.GetBool(AutoRunKey);
            CIBuilder.AutoRunOnBuild = autoRunOnBuild;
            Menu.SetChecked(AutoRunMenuName, autoRunOnBuild);
        }

        [MenuItem(DevMenuName, false, 0)]
        private static void DevelopmentModeMenu()
        {
            var isDevelopment = EditorPrefs.GetBool(DevBuildKey);
            isDevelopment = !isDevelopment;
            EditorUserBuildSettings.development = isDevelopment;
            EditorPrefs.SetBool(DevBuildKey, isDevelopment);
            Menu.SetChecked(DevMenuName, isDevelopment);
        }

        [MenuItem(AutoRunMenuName, false, 0)]
        private static void AutoRunMenu()
        {
            var autoRunOnBuild = EditorPrefs.GetBool(AutoRunKey);
            autoRunOnBuild = !autoRunOnBuild;
            CIBuilder.AutoRunOnBuild = autoRunOnBuild;
            Menu.SetChecked(AutoRunMenuName, autoRunOnBuild);
        }

        [MenuItem("Custom/CI/Build iOS")]
        private static void PerformIOSBuild()
        {
            CIBuilder.PerformIOSBuild();
        }

        [MenuItem("Custom/CI/Build Android")]
        private static void PerformAndroidBuild()
        {
            CIBuilder.PerformAndroidBuild();
        }

        [MenuItem("Custom/CI/Build Android (Export)")]
        private static void PerformAndroidExportBuild()
        {
            CIBuilder.PerformAndroidExportBuild();
        }


        [MenuItem("Custom/CI/Build Android (GPlay)")]
        private static void PerformAndroidPlayStoreBuild()
        {
            CIBuilder.PerformAndroidPlayStoreBuild();
        }

        [MenuItem("Custom/CI/Build and Run Android (GPlay)")]
        private static void PerformAndroidPlayStoreBuildAndRun()
        {
            CIBuilder.PerformAndroidPlayStoreBuildAndRun();
        }

        [MenuItem("Custom/CI/Build macOS (IL2CPP)")]
        private static void PerformMacosBuild()
        {
            CIBuilder.PerformMacosBuild();
        }

        [MenuItem("Custom/CI/Build macOS (Mono)")]
        private static void PerformMacosBuildMono()
        {
            CIBuilder.PerformMacosBuildMono();
        }

        [MenuItem("Custom/CI/Build Linux")]
        private static void PerformLinuxBuild()
        {
            CIBuilder.PerformLinuxBuild();
        }

        [MenuItem("Custom/CI/Build Linux (Mono)")]
        private static void PerformLinuxMonoBuild()
        {
            CIBuilder.PerformLinuxMonoBuild();
        }

        [MenuItem("Custom/CI/Build Windows")]
        private static void PerformWindowsBuild()
        {
            CIBuilder.PerformWindowsBuild();
        }

        [MenuItem("Custom/CI/Build Windows (Mono)")]
        private static void PerformWindowsMonoBuild()
        {
            CIBuilder.PerformWindowsMonoBuild();
        }

        [MenuItem("Custom/CI/Custom Setup", false, 0)]
        private static void ApplyCustomSetup()
        {
            CIBuilder.ApplyCustomSetup();
        }

        [MenuItem("Custom/CI/Validate Scenes", false, 0)]
        private static void ValidateScenes()
        {
            Debug.Log("Scenes for IOS: " + string.Join(",", CIBuilder.GetBuildScenes(BuildTarget.iOS)));
            Debug.Log("Scenes for Android: " + string.Join(",", CIBuilder.GetBuildScenes(BuildTarget.Android)));
            Debug.Log("Scenes for MacOS: " + string.Join(",", CIBuilder.GetBuildScenes(BuildTarget.StandaloneOSX)));
            Debug.Log(
                "Scenes for Win64: " + string.Join(",", CIBuilder.GetBuildScenes(BuildTarget.StandaloneWindows64)));
        }
        
        [MenuItem("Custom/CI/Embed Packages",false,0)]
        private static void PerformEmbedPackages()
        {
            CIBuilder.PerformEmbedPackage();
        }
    }
}