using System;
using System.IO;
using Chickchilinka.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Modules.AuthenticationSystem.Config;

namespace CI.Editor
{
    public static class BuildMenu
    {
        private const string ServerAccountsBaseUrl = "https://sunduchki.online/accounts";
        private const string ServerGameBaseUrl = "https://sunduchki.online/game";
        private const string LocalAccountsBaseUrl = "http://localhost:5092/accounts";
        private const string LocalGameBaseUrl = "http://localhost:5000/game";
        private const string PreloaderScenePath = "Assets/Scenes/Preloader.unity";
        private const string BaseConfigPath = "Assets/Config/BaseUrlConfig.asset";

        [MenuItem("Custom/CI/Build Standalone", false, 1)]
        public static void BuildStandalone()
        {
            var editorConfig = GetFirebaseConfigPath("firebase-editor.json");
            var standaloneConfig = GetFirebaseConfigPath("firebase-standalone.json");
            var androidTarget = GetGoogleServicesPath();
            var desktopTarget = GetDesktopGoogleServicesPath();

            try
            {
                SwapFirebaseConfig(standaloneConfig, androidTarget, updateAndroidXml: true);
                SwapFirebaseConfig(standaloneConfig, desktopTarget, updateAndroidXml: false);
                CIBuilder.PerformWindowsMonoBuild();
            }
            finally
            {
                SwapFirebaseConfig(editorConfig, androidTarget, updateAndroidXml: true);
                SwapFirebaseConfig(editorConfig, desktopTarget, updateAndroidXml: false);
            }
        }

        public static void BuildWebGL()
        { 
            CIBuilder.PerformWebGLBuild();
        }

        [MenuItem("Custom/CI/Build Server WebGL", false, 4)]
        public static void BuildServerWebGL()
        {
            BuildWebGLWithConfig(ServerAccountsBaseUrl, ServerGameBaseUrl);
        }
        
        [MenuItem("Custom/CI/Build Local WebGL", false, 5)]
        public static void BuildLocalWebGL()
        {
            BuildWebGLWithConfig(LocalAccountsBaseUrl, LocalGameBaseUrl);
        }
        
        [MenuItem("Custom/CI/Build Server Standalone", false, 2)]
        public static void BuildServerStandalone()
        {
            BuildStandaloneWithConfig(ServerAccountsBaseUrl, ServerGameBaseUrl);
        }

        [MenuItem("Custom/CI/Build Local Standalone", false, 3)]
        public static void BuildLocalStandalone()
        {
            BuildStandaloneWithConfig(LocalAccountsBaseUrl, LocalGameBaseUrl);
        }

        [MenuItem("Custom/Run/Run Server Game", false, 100)]
        public static void RunServerGame()
        {
            ApplyConfigs(ServerAccountsBaseUrl, ServerGameBaseUrl);
            StartPlayMode();
        }

        [MenuItem("Custom/Run/Run Local Game", false, 101)]
        public static void RunLocalGame()
        {
            ApplyConfigs(LocalAccountsBaseUrl, LocalGameBaseUrl);
            StartPlayMode();
        }

        private static void SwapFirebaseConfig(string sourceRelativePath, string targetRelativePath, bool updateAndroidXml)
        {
            var sourcePath = GetAbsolutePath(sourceRelativePath);
            var targetPath = GetAbsolutePath(targetRelativePath);
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[BuildMenu] Firebase config not found: {sourcePath}");
                return;
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(sourcePath, targetPath, true);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/{targetRelativePath}");
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (updateAndroidXml)
            {
                Firebase.Editor.GenerateXmlFromGoogleServicesJson.ForceJsonUpdate();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static string GetFirebaseConfigPath(string fileName)
        {
            return Path.Combine("Environment", "DEV", "Firebase", fileName);
        }

        private static string GetGoogleServicesPath()
        {
            return Path.Combine("Plugins", "Android", "google-services.json");
        }

        private static string GetDesktopGoogleServicesPath()
        {
            return Path.Combine("StreamingAssets", "google-services-desktop.json");
        }

        private static string GetAbsolutePath(string relativePath)
        {
            return Path.Combine(Application.dataPath, relativePath);
        }

        private static void ApplyConfigs(string accountsBaseUrl, string gameBaseUrl)
        {
            var baseConfig = AssetDatabase.LoadAssetAtPath<BaseUrlConfigAsset>(BaseConfigPath);
            if (baseConfig == null)
            {
                Debug.LogError($"[BuildMenu] BaseUrlConfig asset not found at {BaseConfigPath}.");
            }
            else
            {
                SetSerializedString(baseConfig, "_accountsBaseUrl", accountsBaseUrl);
                SetSerializedString(baseConfig, "_gameBaseUrl", gameBaseUrl);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SetSerializedString(ScriptableObject asset, string fieldName, string value)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[BuildMenu] Field '{fieldName}' not found on {asset.name}.");
                return;
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void StartPlayMode()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var preloaderScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PreloaderScenePath);
                if (preloaderScene == null)
                {
                    Debug.LogError($"[BuildMenu] Preloader scene not found at {PreloaderScenePath}.");
                    return;
                }

                EditorSceneManager.playModeStartScene = preloaderScene;
                EditorApplication.isPlaying = true;
            }
        }

        private static void BuildStandaloneWithConfig(string accountsBaseUrl, string gameBaseUrl)
        {
            var snapshot = CaptureConfigs();
            ApplyConfigs(accountsBaseUrl, gameBaseUrl);

            try
            {
                BuildStandalone();
            }
            finally
            {
                if (snapshot != null)
                {
                    SetBaseUrlOverrides(snapshot);
                }
            }
        }

        private static void BuildWebGLWithConfig(string accountsBaseUrl, string gameBaseUrl)
        {
            var snapshot = CaptureConfigs();
            ApplyConfigs(accountsBaseUrl, gameBaseUrl);
            try
            {
                BuildWebGL();
            }
            finally
            {
                if (snapshot != null)
                {
                    SetBaseUrlOverrides(snapshot);
                }
            }
        }

        private static void SetBaseUrlOverrides(ConfigSnapshot snapshot)
        {
            var baseConfig = AssetDatabase.LoadAssetAtPath<BaseUrlConfigAsset>(BaseConfigPath);
            if (baseConfig == null)
            {
                return;
            }

            SetSerializedString(baseConfig, "_accountsBaseUrl", snapshot.AccountsBaseUrl);
            SetSerializedString(baseConfig, "_gameBaseUrl", snapshot.GameBaseUrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static ConfigSnapshot CaptureConfigs()
        {
            var baseConfig = AssetDatabase.LoadAssetAtPath<BaseUrlConfigAsset>(BaseConfigPath);
            if (baseConfig == null)
            {
                Debug.LogWarning("[BuildMenu] Config assets not found, restore after build is skipped.");
                return null;
            }

            return new ConfigSnapshot
            {
                AccountsBaseUrl = GetSerializedString(baseConfig, "_accountsBaseUrl"),
                GameBaseUrl = GetSerializedString(baseConfig, "_gameBaseUrl")
            };
        }

        private static string GetSerializedString(ScriptableObject asset, string fieldName)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(fieldName);
            return property != null ? property.stringValue : string.Empty;
        }

        private sealed class ConfigSnapshot
        {
            public string AccountsBaseUrl;
            public string GameBaseUrl;
        }
    }
}
