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

using System;
using UnityEditor;
using UnityEngine;

// ReSharper disable StringLiteralTypo

// ReSharper disable once CheckNamespace
namespace ICVR.Tools
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once InconsistentNaming
    [InitializeOnLoad]
    public static class CIBootstrap
    {
        private const string RESOURCES_PATH = "Assets/Resources";
        
        private static readonly string ScriptsFolder;
        private static readonly string PackageFolder;

        static CIBootstrap()
        {
            InitializeConfig();
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            CIUtils.GetCustomEnvironments()?.Initialize();
            
            var currentVersion = VersionUtil.LoadVersionFromPlayerSettings();

            if (!currentVersion.Equals(AppVersion.Current))
            {
                VersionUtil.ApplyVersion(EditorUserBuildSettings.activeBuildTarget, AppVersion.Current);                
            }

            EnvironmentUtil.SelectEnvironment(AppEnvironment.Current);
        }

        private static void InitializeConfig()
        {
            const string buildConfigName = nameof(AppBuildConfig);
            
            if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            var assets = AssetDatabase.FindAssets("t:" + buildConfigName, new[] { RESOURCES_PATH });
            
            AppBuildConfig config = null;
            
            if (assets.Length == 0)
            {
                config = ScriptableObject.CreateInstance<AppBuildConfig>();
                
                try
                {
                    AssetDatabase.CreateAsset(config, RESOURCES_PATH + $"/{buildConfigName}.asset");
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + "\n" + e.StackTrace);
                }
            }
            else
            {
                var path = AssetDatabase.GUIDToAssetPath(assets[0]);
                
                config = AssetDatabase.LoadAssetAtPath<AppBuildConfig>(path);
            }

            if (config != null)
            {
                AppBuildConfig.Initialize(config);
            }
        }

        public static void SaveVersion(Version version)
        {
            AppVersion.ResetVersion(version);
            AppBuildConfig.Instance.SetVersion(version.ToString());
            EditorUtility.SetDirty(AppBuildConfig.Instance);
        }

        public static void SaveEnvironment(AppEnvironment environment)
        {
            AppEnvironment.ResetEnvironment(environment);
            AppBuildConfig.Instance.SetEnvironment(environment.Id);
            EditorUtility.SetDirty(AppBuildConfig.Instance);
        }
    }
}