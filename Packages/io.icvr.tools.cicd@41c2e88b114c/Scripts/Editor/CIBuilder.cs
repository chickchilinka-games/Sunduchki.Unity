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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ICVR.Tools
{
    // ReSharper disable once InconsistentNaming
    public static class CIBuilder
    {
        private const string ApkExt = "apk";
        private const string AabExt = "aab";
        private static readonly string ManifestRelativePath = Path.Combine("Packages", "manifest.json");
        public static bool AutoRunOnBuild = false;

        public static string[] GetBuildScenes(BuildTarget target)
        {
            return CIUtils.GetCustomConfig()?.GetSceneListForTarget(target)
                   ?? FindEnabledEditorScenes();
        }

        private static BuildOptions GetBuildOptions(BuildOptions defaultOptions)
        {
            var optionsString = Environment.GetEnvironmentVariable("CI_BUILD_OPTIONS");
            var options = defaultOptions;
            if (!string.IsNullOrEmpty(optionsString))
            {
                var optionsArray = optionsString.Split(',');
                foreach (var optionKey in optionsArray)
                {
                    if (string.IsNullOrEmpty(optionKey)) continue;
                    if (!Enum.TryParse(optionKey.Trim(), true, out BuildOptions optionValue)) continue;
                    options |= optionValue;
                }
            }

            return options;
        }

        public static void PerformIOSBuild()
        {
            GenericBuild("ios",
                BuildTargetGroup.iOS,
                BuildTarget.iOS);
            ProvideIOSComplianceInfo("ios");
        }

        public static void PerformAndroidBuild()
        {
            ApplyAndroidCommonSettings();
            EditorUserBuildSettings.buildAppBundle = false;
            SetupAndroidx86();
            GenericBuildAndroid($"android/application.{ApkExt}", BuildOptions.None);
        }

        public static void PerformAndroidExportBuild()
        {
            ApplyAndroidCommonSettings();
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            SetupAndroidx64();
            GenericBuildAndroid("android", BuildOptions.None);
        }

        public static void PerformAndroidPlayStoreBuild()
        {
            ApplyAndroidCommonSettings();
            EditorUserBuildSettings.buildAppBundle = true;
            SetupAndroidx64();
            GenericBuildAndroid($"android/application.{AabExt}", BuildOptions.None);
        }
        
        public static void PerformFullAPKBuild()
        {
            ApplyAndroidCommonSettings();
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.splitApplicationBinary = false;
            SetupAndroidx64();
            GenericBuildAndroid($"android/application-full.{ApkExt}", BuildOptions.None);
        }

        public static void PerformAndroidPlayStoreBuildAndRun()
        {
            ApplyAndroidCommonSettings();
            EditorUserBuildSettings.buildAppBundle = true;
            SetupAndroidx64();
            GenericBuildAndroid($"android/application.{AabExt}", BuildOptions.AutoRunPlayer);
        }

        private static void ApplyAndroidCommonSettings()
        {
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            var ndkPath = Environment.GetEnvironmentVariable("ANDROID_NDK_HOME");
            if (!string.IsNullOrEmpty(ndkPath))
            {
                Debug.Log($"[CIBuilder] Setting Android NDK to '{ndkPath}'");
                EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
                EditorPrefs.SetString("AndroidNdkRootR19", ndkPath);
            }

            var keystoreName = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_NAME");
            var keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS") ?? "";
            var keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
            var keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS") ?? "";

            if (!string.IsNullOrEmpty(keyaliasName) && !string.IsNullOrEmpty(keystoreName))
            {
                Debug.Log(
                    $"Applying Android signing:\n\tkeystore={keystoreName}/{new string('*', keystorePass.Length)},\n\tkeyalias={keyaliasName}/{new string('*', keyaliasPass.Length)}");
                PlayerSettings.Android.keystoreName = keystoreName;
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.keyaliasName = keyaliasName;
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }
        }

        public static void PerformMacosBuild()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

#if UNITY_2020_2_OR_NEWER
            EditorUserBuildSettings.SetPlatformSettings(
                BuildTargetGroup.Standalone.ToString(),
                "OSXUniversal",
                "Architecture",
                "x64");
#endif

            GenericBuild(
                "macos/application",
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneOSX);
        }

        public static void PerformMacosBuildMono()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

#if UNITY_2020_2_OR_NEWER
            EditorUserBuildSettings.SetPlatformSettings(
                BuildTargetGroup.Standalone.ToString(),
                "OSXUniversal",
                "Architecture",
                "x64");
#endif

            GenericBuild(
                "macos/application",
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneOSX);
        }

        public static void PerformLinuxBuild()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

            GenericBuild("linux/application",
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneLinux64);
        }

        public static void PerformLinuxMonoBuild()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            GenericBuild("linux/application",
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneLinux64);
        }

        public static void PerformWindowsBuild()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

            GenericBuild("windows/application.exe",
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
        }

        public static void PerformWindowsMonoBuild()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            GenericBuild("windows/application.exe",
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
        }

        public static void ApplyCustomSetup()
        {
            CIUtils.GetCustomConfig()?.ApplyCustomSetup();
        }

        public static void PerformEmbedPackage()
        {
            ApplyEmbedPackage(new[] { "ICVR" });
        }

        public static void GenericBuildAndroid(string targetFilePath, BuildOptions buildOptions)
        {
            GenericBuild(targetFilePath,
                BuildTargetGroup.Android,
                BuildTarget.Android,
                buildOptions);
        }

        public static void SetupAndroidx86()
        {
            Debug.Log("AndroidArchitecture.X86 is obsolete, switching to AndroidArchitecture.X64");
            SetupAndroidx64();
        }

        public static void SetupAndroidx64()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        }

        public static string[] FindEnabledEditorScenes()
        {
            var editorScenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                editorScenes.Add(scene.path);
            }

            return editorScenes.ToArray();
        }

        public static void GenericBuild(string targetFilePath,
            BuildTargetGroup buildTargetGroup,
            BuildTarget buildTarget,
            BuildOptions buildOptions = BuildOptions.None)
        {
            var scenes = GetBuildScenes(buildTarget);
            var fullOptions = GetBuildOptions(buildOptions);
            GenericBuild(scenes, targetFilePath, buildTargetGroup, buildTarget, fullOptions);
        }

        public static void GenericBuild(string[] scenes, string targetFilePath,
            BuildTargetGroup buildTargetGroup,
            BuildTarget buildTarget,
            BuildOptions buildOptions)
        {
            ChangePlatform(buildTargetGroup, buildTarget);

            var versionText = Environment.GetEnvironmentVariable("CI_VERSION");
            
            Debug.Log($"Get version from CI: {versionText}");

            if (!string.IsNullOrEmpty(versionText))
            {
                VersionUtil.SetVersionFromString(versionText);
                
                Debug.Log($"Version is set: {AppVersion.Current}");
            }
            
            var appId = ApplyAppId(buildTargetGroup);

            PlayerSettings.SplashScreen.showUnityLogo = false;

            if (!ValidateScenePaths(scenes)) FailBuild("Build error: invalid scene configuration");
            if (!IsPlatformSupported(buildTarget)) FailBuild($"Build error: platform not supported {buildTarget}");

            var appEnvironment = EnvironmentUtil.GetSelectedEnvironment();

            var envId = Environment.GetEnvironmentVariable("CI_ENV");
            if (envId != null) appEnvironment = AppEnvironment.FromId(envId);

            Debug.Log($"Apply version {AppVersion.Current} before build");
            
            VersionUtil.ApplyVersion(buildTarget, AppVersion.Current);
            EnvironmentUtil.SelectEnvironment(appEnvironment,
                () => BuildPlayer(scenes, appId, targetFilePath, buildTargetGroup, buildTarget, buildOptions, appEnvironment),
                () => Debug.Log($"[CI] Build failed! Wrong credentials!"),
                true);
        }

        private static void BuildPlayer(string[] scenes,
                                        string appId,
                                        string targetFilePath,
                                        BuildTargetGroup buildTargetGroup,
                                        BuildTarget buildTarget,
                                        BuildOptions buildOptions,
                                        AppEnvironment appEnvironment)
        {
            ApplyCustomSetup();

            if (EditorUserBuildSettings.development) buildOptions |= BuildOptions.Development;
            if (AutoRunOnBuild && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITLAB_CI")))
                buildOptions |= BuildOptions.AutoRunPlayer;

            if (Environment.GetEnvironmentVariable("CI_SKIP_BUILD") != null)
            {
                Debug.Log("[CIBuilder] skipping build pipeline... success!");
                return;
            }

            var buildReport = BuildPipeline.BuildPlayer(scenes, targetFilePath, buildTarget, buildOptions);
            var isTargetFileExist = targetFilePath.Contains('.') ? File.Exists(targetFilePath) : Directory.Exists(targetFilePath); 
            
            if (buildReport.summary.result == BuildResult.Succeeded && isTargetFileExist)
            {
                Debug.Log("[CIBuilder] Build success!");
                SaveBuildSummary(targetFilePath, appId, appEnvironment, buildTarget,
                    buildTargetGroup,
                    buildOptions);
                SaveBuildReport(targetFilePath, buildReport);
            }
            else
            {
                var failBuildSummary =
                    $"with {buildReport.summary.totalErrors} errors, {buildReport.summary.totalWarnings} warnings";
                var notFoundedBuildSummary = $"Target file wasn't found in the path {targetFilePath}";
                
                FailBuild(
                    $"Build error! {buildReport.summary.result} " + (isTargetFileExist ? failBuildSummary : notFoundedBuildSummary));
            }

            CIUtils.GetCustomConfig()?.PostProcessBuild();
        }
        
        private static void SaveBuildReport(string targetDir, BuildReport buildReport)
        {
            var output = new StringBuilder();
            output.AppendLine($"Build Report at {DateTime.Now:U}");
            output.AppendLine("-----");

            ulong totalFileSize = 0;
            var roles = new Dictionary<string, ulong>();
            foreach (var buildFile in buildReport.GetFiles())
            {
                if (roles.TryGetValue(buildFile.role, out var roleSize))
                    roles[buildFile.role] = roleSize + buildFile.size;
                else
                    roles[buildFile.role] = buildFile.size;
                totalFileSize += buildFile.size;
            }

            output.AppendLine("File size by Role:");
            foreach (var roleData in roles.OrderByDescending(x => x.Value))
                output.AppendLine(
                    $"{roleData.Key.PadRight(30)} {FormatSize(roleData.Value)} ({roleData.Value / (float)totalFileSize:P1})");

            output.AppendLine($"Total files size: {FormatSize(totalFileSize)}");

            output.AppendLine("-----");
            var filesSorted = buildReport.packedAssets
                .SelectMany(x => x.contents)
                .OrderByDescending(x => x.packedSize)
                .ToArray();

            var totalAssetSize = (ulong)filesSorted.Sum(x => (long)x.packedSize);

            output.AppendLine("Assets sorted by size:");
            foreach (var packedAsset in filesSorted)
                output.AppendLine(
                    $"{FormatSize(packedAsset.packedSize)} ({packedAsset.packedSize / (float)totalAssetSize:P1})    {packedAsset.sourceAssetPath}");
            output.AppendLine($"Total asset size: {FormatSize(totalAssetSize)}");

            var filePath = GetFileInBuildDir(targetDir, "buildReport.txt");
            File.WriteAllText(filePath, output.ToString());
        }

        private static string FormatSize(ulong size)
        {
            var scale = "b";
            var outSize = (float)size;
            if (outSize >= 1024)
            {
                outSize /= 1024;
                scale = "Kb";
            }

            if (outSize >= 1024)
            {
                outSize /= 1024;
                scale = "Mb";
            }

            return $"{outSize:N1}{scale}";
        }

        private static void FailBuild(string message)
        {
            Debug.LogError("[CIBuilder] " + message);
            throw new BuildFailedException(message);
        }

        private static void SaveBuildSummary(string targetDir, string appId, AppEnvironment environment,
            BuildTarget buildTarget,
            BuildTargetGroup buildTargetGroup, BuildOptions buildOptions)
        {
            var summary = new VersionSummary
            {
                buildDate = DateTime.Now.ToUniversalTime().ToString("O"),
                appId = appId,
                version = AppVersion.FormatToString(),
                environment = environment.Id,
                buildTarget = buildTarget.ToString(),
                buildTargetGroup = buildTargetGroup.ToString(),
                scriptingBackend = PlayerSettings.GetScriptingBackend(buildTargetGroup).ToString(),
                buildType = EditorUserBuildSettings.development ? "Development" : "Release",
                buildOptions = GetBuildOptionsString(buildOptions)
            };

            var filePath = GetFileInBuildDir(targetDir, "summary.json");
            var fileContents = JsonUtility.ToJson(summary, true);
            Console.WriteLine("Build Summary: " + fileContents);
            File.WriteAllText(filePath, fileContents);
        }

        private static string GetFileInBuildDir(string targetDir, string fileName)
        {
            var buildDir = Directory.GetParent(targetDir)?.FullName;
            if (buildDir == null) return null;
            if (!Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);
            return Path.Combine(buildDir, fileName);
        }

        private static string GetBuildOptionsString(BuildOptions buildOptions)
        {
            var buildValues = Enum.GetValues(typeof(BuildOptions));
            var output = new StringBuilder();
            foreach (BuildOptions buildOption in buildValues)
                if (((int)buildOption & (int)buildOptions) != 0)
                {
                    if (output.Length != 0) output.Append(", ");
                    output.Append(buildOption.ToString());
                }

            return output.ToString();
        }

        private static string ApplyAppId(BuildTargetGroup buildTarget)
        {
            var appId = Environment.GetEnvironmentVariable("APP_ID");
            if (!string.IsNullOrEmpty(appId))
            {
                Debug.Log($"[CIBuilder] Applying app id {appId} for target {buildTarget}");
                PlayerSettings.SetApplicationIdentifier(buildTarget, appId);
            }
            else
            {
                appId = PlayerSettings.GetApplicationIdentifier(buildTarget);
            }

            return appId;
        }

        public static void ChangePlatform(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            Debug.Log($"[CIBuilder] Change build target {buildTargetGroup} {buildTarget}");
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        }

        public static bool ChangePlatform(BuildTarget buildTarget)
        {
            var buildTargetGroup = GetBuildTargetGroup(buildTarget);
            if (buildTargetGroup == BuildTargetGroup.Unknown)
            {
                Debug.LogWarning($"[CIBuilder] Build target group for target {buildTarget} is unknown, skipping");
                return false;
            }

            if (!IsPlatformSupported(buildTarget))
            {
                Debug.LogWarning($"[CIBuilder] Build target {buildTarget} is not available, skipping");
                return false;
            }

            ChangePlatform(buildTargetGroup, buildTarget);
            return true;
        }

        public static void ApplyEmbedPackage(string[] scopedRegistryNames)
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectPath)) return;

            var serializedManifestData = File.ReadAllText(Path.Combine(projectPath, ManifestRelativePath));

            try
            {
                var manifestData = JsonConvert.DeserializeObject<ManifestDataSerializable>(serializedManifestData);
                if (manifestData == null) return;

                var scopesToEmbed = manifestData.scopedRegistries
                    .Where(scope => scopedRegistryNames.Any(item =>
                        string.Equals(scope.name, item, StringComparison.InvariantCultureIgnoreCase)))
                    .SelectMany(scope => scope.scopes)
                    .ToList();
                if (scopesToEmbed.Count == 0) return;

                var packagesToEmbed = manifestData.dependencies.Keys
                    .Where(packageName =>
                        scopesToEmbed.Any(item =>
                            packageName.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) >= 0))
                    .ToList();
                if (packagesToEmbed.Count == 0) return;

                foreach (var packageName in packagesToEmbed)
                {
                    var embedRequest = Client.Embed(packageName);
                    while (!embedRequest.IsCompleted)
                    {
                        if (embedRequest.Status >= StatusCode.Failure)
                            Debug.LogError(
                                $"[CIBuilder] Package {packageName} embed failed. Message: {embedRequest.Error.message}");
                    }

                    if (embedRequest.IsCompleted && embedRequest.Status == StatusCode.Success)
                    {
                        Debug.Log($"[CIBuilder] Package {packageName} embedded");
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[CIBuilder] Embed Package failed. Message:" + exception.Message);
            }
        }

        private static bool IsPlatformSupported(BuildTarget buildTarget)
        {
            // unofficial!
            var moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
            if (moduleManager == null)
                throw new Exception("Can't find internal method: UnityEditor.Modules.ModuleManager,UnityEditor.dll");
            var isPlatformSupportLoaded =
                moduleManager.GetMethod("IsPlatformSupportLoaded", BindingFlags.Static | BindingFlags.NonPublic);
            if (isPlatformSupportLoaded == null)
                throw new Exception(
                    "Can't find internal method: UnityEditor.Modules.ModuleManager.isPlatformSupportLoaded()x");
            var getTargetStringFromBuildTarget = moduleManager.GetMethod("GetTargetStringFromBuildTarget",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (getTargetStringFromBuildTarget == null)
                throw new Exception(
                    "Can't find internal method: UnityEditor.Modules.ModuleManager.getTargetStringFromBuildTarget()");
            return (bool)isPlatformSupportLoaded.Invoke(null,
                new object[] { (string)getTargetStringFromBuildTarget.Invoke(null, new object[] { buildTarget }) });
        }

        private static bool ValidateScenePaths(string[] scenes)
        {
            if (scenes == null || scenes.Length == 0)
            {
                Debug.LogError("Scenes array is empty");
                return false;
            }

            foreach (var scenePath in scenes)
            {
                var projectPath = Directory.GetParent(Application.dataPath)?.FullName;
                if (projectPath == null)
                    throw new InvalidOperationException(
                        $"Can't get parent directory from Application.dataPath={Application.dataPath}");
                var fullPath = Path.Combine(projectPath, scenePath);
                if (File.Exists(fullPath)) continue;
                Debug.LogError($"Invalid scene path: '{scenePath}'");
                return false;
            }

            return true;
        }


        private static void ProvideIOSComplianceInfo(string path)
        {
            var plistPath = Path.Combine(path, "Info.plist");
            Debug.Log($"ProvideIOSComplianceInfo in file {plistPath}");
#if UNITY_IOS
            var plist = new UnityEditor.iOS.Xcode.PlistDocument();
            plist.ReadFromFile(plistPath);
            var rootDict = plist.root;
            rootDict.SetString("ITSAppUsesNonExemptEncryption", "false");
            File.WriteAllText(plistPath, plist.WriteToString());
#endif
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android: return BuildTargetGroup.Android;
                case BuildTarget.iOS: return BuildTargetGroup.iOS;
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                    return BuildTargetGroup.Standalone;
                default:
                    return BuildTargetGroup.Unknown;
            }
        }

        [Serializable]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private struct VersionSummary
        {
            [SerializeField] public string buildDate;
            [SerializeField] public string appId;
            [SerializeField] public string version;
            [SerializeField] public string environment;
            [SerializeField] public string buildTarget;
            [SerializeField] public string buildTargetGroup;
            [SerializeField] public string scriptingBackend;
            [SerializeField] public string buildType;
            [SerializeField] public string buildOptions;
        }

        public class ManifestDataSerializable
        {
            public Dictionary<string, string> dependencies;
            public ScopedRegistryDataSerializable[] scopedRegistries;
        }

        public class ScopedRegistryDataSerializable
        {
            public string name;
            public string url;
            public string[] scopes;
        }
    }
}