using System.IO;
using System.Reflection;
using ICVR.Tools;
using MarrowMachine.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Modules.Lobby.Config;
using Modules.Profiles.Config;
using Modules.SignalR.Config;

namespace CI.Editor
{
    public static class BuildMenu
    {
        private const string ServerBaseUrl = "https://sunduchki.online";
        private const string LocalGameBaseUrl = "http://localhost:5000/game";
        private const string LocalAccountsBaseUrl = "http://localhost:5092/accounts";
        private const string PreloaderScenePath = "Assets/Scenes/Preloader.unity";
        private const string LobbyConfigPath = "Assets/Config/LobbyApiConfigProvider.asset";
        private const string ProfileConfigPath = "Assets/Config/ProfileApiConfigProvider.asset";
        private const string HubConfigPath = "Assets/Config/GameHubConfig.asset";

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

        [MenuItem("Custom/CI/Build Server Standalone", false, 2)]
        public static void BuildServerStandalone()
        {
            BuildStandaloneWithConfig(
                $"{ServerBaseUrl}/game",
                $"{ServerBaseUrl}/accounts",
                $"{ServerBaseUrl}/game/hub/game");
        }

        [MenuItem("Custom/CI/Build Local Standalone", false, 3)]
        public static void BuildLocalStandalone()
        {
            BuildStandaloneWithConfig(
                LocalGameBaseUrl,
                LocalAccountsBaseUrl,
                $"{LocalGameBaseUrl}/hub/game");
        }

        [MenuItem("Custom/Run/Run Server Game", false, 100)]
        public static void RunServerGame()
        {
            ApplyConfigs(
                $"{ServerBaseUrl}/game",
                $"{ServerBaseUrl}/accounts",
                $"{ServerBaseUrl}/game/hub/game");
            StartPlayMode();
        }

        [MenuItem("Custom/Run/Run Local Game", false, 101)]
        public static void RunLocalGame()
        {
            ApplyConfigs(
                LocalGameBaseUrl,
                LocalAccountsBaseUrl,
                $"{LocalGameBaseUrl}/hub/game");
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

        private static void ApplyConfigs(string lobbyBaseUrl, string profileBaseUrl, string hubUrl)
        {
            var lobbyConfig = AssetDatabase.LoadAssetAtPath<LobbyApiConfigProviderAsset>(LobbyConfigPath);
            if (lobbyConfig == null)
            {
                Debug.LogError($"[BuildMenu] LobbyApiConfig asset not found at {LobbyConfigPath}.");
            }
            else
            {
                SetSerializedString(lobbyConfig, "_baseAddress", lobbyBaseUrl);
            }

            var profileConfig = AssetDatabase.LoadAssetAtPath<ProfileApiConfigProviderAsset>(ProfileConfigPath);
            if (profileConfig == null)
            {
                Debug.LogError($"[BuildMenu] ProfileApiConfig asset not found at {ProfileConfigPath}.");
            }
            else
            {
                SetSerializedString(profileConfig, "_baseAddress", profileBaseUrl);
            }

            var hubConfig = AssetDatabase.LoadAssetAtPath<GameHubConfigProviderAsset>(HubConfigPath);
            if (hubConfig == null)
            {
                Debug.LogError($"[BuildMenu] GameHubConfig asset not found at {HubConfigPath}.");
            }
            else
            {
                SetSerializedString(hubConfig, "_hubUrl", hubUrl);
                ResetCachedHubUri(hubConfig);
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

        private static void ResetCachedHubUri(GameHubConfigProviderAsset asset)
        {
            var field = typeof(GameHubConfigProviderAsset)
                .GetField("_cachedUri", BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(asset, null);
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

        private static void BuildStandaloneWithConfig(string lobbyBaseUrl, string profileBaseUrl, string hubUrl)
        {
            var snapshot = CaptureConfigs();
            ApplyConfigs(lobbyBaseUrl, profileBaseUrl, hubUrl);

            try
            {
                BuildStandalone();
            }
            finally
            {
                if (snapshot != null)
                {
                    ApplyConfigs(snapshot.LobbyBaseUrl, snapshot.ProfileBaseUrl, snapshot.HubUrl);
                }
            }
        }

        private static ConfigSnapshot CaptureConfigs()
        {
            var lobbyConfig = AssetDatabase.LoadAssetAtPath<LobbyApiConfigProviderAsset>(LobbyConfigPath);
            var profileConfig = AssetDatabase.LoadAssetAtPath<ProfileApiConfigProviderAsset>(ProfileConfigPath);
            var hubConfig = AssetDatabase.LoadAssetAtPath<GameHubConfigProviderAsset>(HubConfigPath);

            if (lobbyConfig == null || profileConfig == null || hubConfig == null)
            {
                Debug.LogWarning("[BuildMenu] Config assets not found, restore after build is skipped.");
                return null;
            }

            return new ConfigSnapshot
            {
                LobbyBaseUrl = GetSerializedString(lobbyConfig, "_baseAddress"),
                ProfileBaseUrl = GetSerializedString(profileConfig, "_baseAddress"),
                HubUrl = GetSerializedString(hubConfig, "_hubUrl")
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
            public string LobbyBaseUrl;
            public string ProfileBaseUrl;
            public string HubUrl;
        }
    }
}
